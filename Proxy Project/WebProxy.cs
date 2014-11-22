using System;
using System.Threading;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Collections.Generic;


namespace HttpProxy
{
    public delegate void WebEvent(WebProxy sender, string id);

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
<<<<<<< Updated upstream
                    //handler.socket.Close(5);
=======
>>>>>>> Stashed changes
                    handler.socket.Dispose();
                }

                clients.Clear();
<<<<<<< Updated upstream
                OnConnectionEnd();
=======
                OnConnectionEnd(new ExtendedSocket());
>>>>>>> Stashed changes
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
>>>>>>> Stashed changes
        }


       
        private Socket listener;
        private List<ExtendedSocket> clients;

        /* should store mappings of client string (aka RemoteEndpoint for a given socket) 
           to the Extendedsocket its contained in  */
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
                    client_info.Add(id, clientHandler);
                    OnConnectionEstablished(id);

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
                    // && cHandler.State() == ExtendedSocket.SocketState.Open)
                    while (bytesRead > 0 )
                    {
                        cHandler.socket.Send(buffer, 0, bytesRead, SocketFlags.None);
                        bytesRead = wHandler.Receive(buffer);
                    }

                    OnResponseReceived();
<<<<<<< Updated upstream
=======

                    wHandler.Shutdown(SocketShutdown.Both);
                    wHandler.Close();
                    cHandler.whosts.Remove(wHandler); 
>>>>>>> Stashed changes
                }
                catch (Exception exc)
                {
                    lastException = exc.ToString();
                    OnExceptionThrown();
                }
            }

            wHandler.Shutdown(SocketShutdown.Both);
            //wHandler.Close();
            wHandler.Dispose();

            cHandler.Workers--;
            buffer = null;
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
                            OnWebSocketCreated(cHandler, wHandler);
                        }
                        else
                        {
                            Regex hostAndPortParser = new Regex(pattern2);
                            host = hostAndPortParser.Match(lastRequest).Groups[1].Value;
                            port = int.Parse(hostAndPortParser.Match(lastRequest).Groups[2].Value);
                            wHandler = CreateWebSocket(host, port);
                            OnWebSocketCreated(cHandler, wHandler);
                        }

                        cHandler.Workers++;
                        byte[] buffernew = new byte[1048576];

                        wHandler.Send(buffer, bytesRead, SocketFlags.None);
                        wHandler.BeginReceive(buffernew, 0, buffernew.Length, SocketFlags.None, new AsyncCallback(ReceiveResponses), new object[] { buffernew, wHandler, cHandler, 5 });
                        
                        cHandler.socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveRequests), new object[] { buffer, cHandler });
                        return;
                    }
                    else
                    {
                        while (cHandler.Workers != 0)
                            Thread.Sleep(10);
                    }

                        try
                        {
                            if ( cHandler.socket.Connected )
                                cHandler.socket.Shutdown(SocketShutdown.Both);
                            clients.Remove(cHandler);
                            cHandler.socket.Dispose();
                            cHandler = null;
                            

                            OnConnectionEnd(cHandler);
                        }
                        catch (Exception exc)
                        {
                            lastException = exc.ToString();
                            OnExceptionThrown();
                        }
                    }
>>>>>>> Stashed changes
                }
                catch (Exception exc)
                {
                    lastException = exc.ToString();
                    OnExceptionThrown();
                }
            }

            cHandler.State(ExtendedSocket.SocketState.Closed);
            buffer = null;
            clients.Remove(cHandler);

            OnConnectionEnd();

            if (cHandler.socket.Connected)
                cHandler.socket.Shutdown(SocketShutdown.Both);
            //cHandler.socket.Close(5);
            cHandler.socket.Dispose();
        }

        public event WebEvent ConnectionEstablished;
        public event WebEvent ConnectionEnd;
        public event WebEvent RequestReceived;
        public event WebEvent ResponseReceived;
        public event WebEvent ExceptionThrown;
       // public event WebEvent WebSocketCreated;

        protected virtual void OnWebSocketCreated(ExtendedSocket client, Socket webhost)
        {
            //if (WebSocketCreated != null)
           // {
                client.whosts.Add(webhost);
                //WebSocketCreated(this, new object[] {client, webhost});
            //}
                
        }
       
        // This should start a thread in Form1 to keep a watch for new clients. Mmmmm Asynchronous yumminess
        protected virtual void OnConnectionEstablished(string id)
        {
            if (ConnectionEstablished != null)
                ConnectionEstablished(this, id);
        }
        protected virtual void OnConnectionEnd(ExtendedSocket client)
        {
            if (ConnectionEnd != null)
                ConnectionEnd(this,new object[] { client } );
        }
        protected virtual void OnRequestReceived(string req)
        {
            if (RequestReceived != null)
                RequestReceived(this,new object[] {req});
        }
        protected virtual void OnResponseReceived()
        {
            if (ResponseReceived != null)
                ResponseReceived(this,null);
        }
        protected virtual void OnExceptionThrown()
        {
            if (ExceptionThrown != null)
                ExceptionThrown(this,null);
        }
    }
}
