using System;
using System.Threading;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Collections.Generic;


namespace HttpProxy
{
    public delegate void WebEvent(WebProxy sender, object[] info);

    public class ExtendedSocket
    {
        public ExtendedSocket()
        {
            whosts = new List<Socket>();
        }
        public enum SocketState { Open, Closed, HalfOpen };
        public Socket socket;

        public List<Socket> whosts;
        /*public int Whosts()
        { return whosts.Count; }*/

        private SocketState state;
        public void State(SocketState s)
        { state = s; }
        public SocketState State()
        { return state; }
    }


    public class WebProxy
    {
        public WebProxy()
        {
            lastRequest = string.Empty;
            clients = new List<ExtendedSocket>();

            serverRunning = false;
        }
        public void Start(IPAddress ip, int port)
        {
            try
            {
                SocketPermission permission = new SocketPermission(NetworkAccess.Accept, TransportType.Tcp, "", SocketPermission.AllPorts);
                IPEndPoint ipEndPoint = new IPEndPoint(ip, port);

                listener = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                localAddress = ip;

                permission.Demand();
                listener.Bind(ipEndPoint);
                listener.Listen(10);

                serverRunning = true;
                listener.BeginAccept(new AsyncCallback(this.AcceptConnection), null); // listener);
            }
            catch (Exception exc)
            {
                lastException = exc.ToString();
                OnExceptionThrown();
            }
        }
        public void Stop()
        {
            serverRunning = false;
            try
            {
                if (listener.Connected)
                    listener.Shutdown(SocketShutdown.Both);

                listener.Dispose();
                listener = null;

                foreach (ExtendedSocket handler in clients)
                {
                    handler.State(ExtendedSocket.SocketState.Closed);

                    if(handler.socket.Connected)
                        handler.socket.Shutdown(SocketShutdown.Both);

                    handler.socket.Dispose();
                }

                clients.Clear();

            }
            catch (Exception exc)
            {
                lastException = exc.ToString();
                OnExceptionThrown();
            }
        }
        public string LastRequest
        {
            get { return lastRequest; }
        }
        public string LastException
        {
            get { return lastException; }
        }
        public int ConnectedClients
        {
            get { return clients.Count; }
        }
        public bool ServerRunning
        { 
            get {return serverRunning; } 
        }


       
        private Socket listener;
        private List<ExtendedSocket> clients;

        /* Mappings of client string (aka RemoteEndpoint for a given socket) 
           to the Extendedsocket its contained in
         * Currently not being used*/
        public Dictionary<String, ExtendedSocket> client_info;


        private IPAddress localAddress;
        private string lastRequest;
        private string lastException;
        private bool serverRunning;


        private Socket CreateWebSocket(string hostname, int portnumber = 80)
        {
            SocketPermission permission = new SocketPermission(NetworkAccess.Connect, TransportType.Tcp, "", SocketPermission.AllPorts);
            Socket webHandler = new Socket(localAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            permission.Demand();
            webHandler.Connect(hostname, portnumber);
            return webHandler;
        }
        private void AcceptConnection(IAsyncResult ar)
        {
            if (serverRunning)
            {
                try
                {
                    string id;
                    ExtendedSocket clientHandler = new ExtendedSocket();
                    clientHandler.socket = listener.EndAccept(ar);
                    clientHandler.State(ExtendedSocket.SocketState.Open);
                    clients.Add(clientHandler);
                    id = clientHandler.socket.RemoteEndPoint.ToString().Split(':')[0];
                    OnConnectionEstablished(clientHandler.socket.RemoteEndPoint.ToString());

                    byte[] buffer = new byte[1048576];

                    clientHandler.socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveRequests), new object[] { buffer, clientHandler });
                    listener.BeginAccept(new AsyncCallback(AcceptConnection), null);
                }
                catch (Exception exc)
                {
                    lastException = exc.ToString();
                    OnExceptionThrown();
                }
            }          
        }
        private void ReceiveResponses(IAsyncResult ar)
        {
            byte[] buffer = (byte[])((object[])ar.AsyncState)[0];
            Socket wHandler = (Socket)((object[])ar.AsyncState)[1];
            ExtendedSocket cHandler = (ExtendedSocket)((object[])ar.AsyncState)[2];

            if (serverRunning && cHandler.State() == ExtendedSocket.SocketState.Open)
            {
                try
                {
                    int bytesRead = wHandler.EndReceive(ar);
                    while (bytesRead > 0 )
                    {
                        cHandler.socket.Send(buffer, 0, bytesRead, SocketFlags.None);
                        bytesRead = wHandler.Receive(buffer);
                    }

                    OnResponseReceived();
                    OnWebHostRemoved(cHandler.socket.RemoteEndPoint.ToString(), wHandler.RemoteEndPoint.ToString());

                    wHandler.Shutdown(SocketShutdown.Both);
                    wHandler.Dispose();
                    cHandler.whosts.Remove(wHandler); 
                    buffer = null;

                }
                catch (Exception exc)
                {
                    lastException = exc.ToString();
                    OnExceptionThrown();
                }
            }
        }
        private void ReceiveRequests(IAsyncResult ar)
        {
            byte[] buffer = (byte[])((object[])ar.AsyncState)[0];
            ExtendedSocket cHandler = (ExtendedSocket)((object[])ar.AsyncState)[1];

            if (serverRunning)
            {
                try
                {
                    int bytesRead = cHandler.socket.EndReceive(ar);

                    if (bytesRead > 0)
                    {
                        String pattern1 = @"Host: ([^:]*?)\r\n";
                        String pattern2 = @"Host: (.*?):([\d]{1,5})\r\n";
                        Regex hostParser = new Regex(pattern1);
                        Socket wHandler;
                        String host;
                        
                        int port;

                        lastRequest = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                        OnRequestReceived(lastRequest);

                        if (hostParser.IsMatch(lastRequest))
                        {
                            host = hostParser.Match(lastRequest).Groups[1].Value;
                            wHandler = CreateWebSocket(host);
                            OnWebSocketCreated(cHandler.socket.RemoteEndPoint.ToString(), wHandler);         
                        }
                        else
                        {
                            Regex hostAndPortParser = new Regex(pattern2);
                            host = hostAndPortParser.Match(lastRequest).Groups[1].Value;
                            port = int.Parse(hostAndPortParser.Match(lastRequest).Groups[2].Value);
                            wHandler = CreateWebSocket(host, port);
                            cHandler.whosts.Add(wHandler);
                            OnWebSocketCreated(cHandler.socket.RemoteEndPoint.ToString(), wHandler);    
                        }

                        byte[] buffernew = new byte[1048576];

                        wHandler.Send(buffer, bytesRead, SocketFlags.None);
                        wHandler.BeginReceive(buffernew, 0, buffernew.Length, SocketFlags.None, new AsyncCallback(ReceiveResponses), new object[] { buffernew, wHandler, cHandler, 5 });
                        
                        cHandler.socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveRequests), new object[] { buffer, cHandler });
                        return;
                    }
                    else
                    {
                        while (cHandler.whosts.Count != 0)
                            Thread.Sleep(10);
                    }

                    OnClientRemoved(cHandler.socket.RemoteEndPoint.ToString());
                    if ( cHandler.socket.Connected )
                        cHandler.socket.Shutdown(SocketShutdown.Both);
                    clients.Remove(cHandler);
                    cHandler.socket.Dispose();
                    cHandler = null;
                    
                }
                catch (Exception exc)
                {
                    lastException = exc.ToString();
                    OnExceptionThrown();
                }
            }
        }


        public event WebEvent ConnectionEstablished;
        // public event WebEvent ConnectionEnd;
        public event WebEvent ClientRemoved;
        public event WebEvent WebHostRemoved;
        public event WebEvent RequestReceived;
        public event WebEvent ResponseReceived;
        public event WebEvent ExceptionThrown;
        public event WebEvent WebSocketCreated;

        protected virtual void OnWebSocketCreated(string client_id, Socket webhost)
        {
            if (WebSocketCreated != null)
            {
                WebSocketCreated(this, new object[] {client_id, webhost});
            }
                
        }
        protected virtual void OnConnectionEstablished(string id)
        {
            if (ConnectionEstablished != null)
                ConnectionEstablished(this, new object[] {id});
        }
        protected virtual void OnRequestReceived(string req)
        {
            if (RequestReceived != null)
                RequestReceived(this, new object[] { req });
        }
        protected virtual void OnResponseReceived()
        {
            if (ResponseReceived != null)
                ResponseReceived(this, null);
        }
        protected virtual void OnClientRemoved(string client_id)
        {
            if (ClientRemoved != null)
                ClientRemoved(this, new object[] {client_id} );
        }
        protected virtual void OnWebHostRemoved(string client_id, string web_id)
        {
            if (WebHostRemoved != null)
                WebHostRemoved(this, new object[] {client_id,web_id});
        }
       /* protected virtual void OnConnectionEnd()
        {
            if (ConnectionEnd != null)
                ConnectionEnd(this,null);
        }*/
        protected virtual void OnExceptionThrown()
        {
            if (ExceptionThrown != null)
                ExceptionThrown(this,null);
        }
    }
}
