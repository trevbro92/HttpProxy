using System;
using System.Threading;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Collections.Generic;


namespace HttpProxy
{
    //----------------------------------------//
    // Definitions for Proxy event data types //
    //----------------------------------------//
    public class ProxyEventArgs
    {
        public Socket[] sockets;
        public string EventText;
    }
    public delegate void WebEvent(WebProxy sender, ProxyEventArgs e);

    public class ExtendedSocket
    {
        public ExtendedSocket()
        {
            workers = new List<Socket>();
        }
        
        public Socket socket;
        public List<Socket> workers;
    }

    public class WebProxy
    {
        //---------------//
        // Proxy methods //
        //---------------//
        public WebProxy()
        {
            clients = new List<ExtendedSocket>();
            ServerRunning = false;
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

                ServerRunning = true;
                listener.BeginAccept(new AsyncCallback(this.AcceptConnection), null); 
            }
            catch (Exception exc)
            {
                OnExceptionThrown(exc.ToString());
            }
        }
        public void Stop()
        {
            ServerRunning = false;

            try
            {
                if (listener.Connected)
                    listener.Shutdown(SocketShutdown.Both);

                listener.Dispose();
                listener = null;

                foreach (ExtendedSocket handler in clients)
                {
                    if(handler.socket.Connected)
                        handler.socket.Shutdown(SocketShutdown.Both);

                    handler.socket.Dispose();
                }

                clients.Clear();

            }
            catch (Exception exc)
            {
                OnExceptionThrown(exc.ToString());
            }
        }
        private Socket CreateWebSocket(string hostname, int portnumber = 80)
        {
            SocketPermission permission = new SocketPermission(NetworkAccess.Connect, TransportType.Tcp, "", SocketPermission.AllPorts);
            Socket webHandler = new Socket(localAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            permission.Demand();
            webHandler.Connect(hostname, portnumber);
            return webHandler;
        }

        //--------------------------//
        // Proxy members/properties //
        //--------------------------//
        public int ConnectedClients
        {
            get { return clients.Count; }
        }
        public bool ServerRunning
        {
            get;
            set;
        }
        private Socket listener;
        private List<ExtendedSocket> clients;
        private IPAddress localAddress;

        //------------------------------//
        // Asyncronous Socket callbacks //
        //------------------------------//
        private void AcceptConnection(IAsyncResult ar)
        {
            if (ServerRunning)
            {
                try
                {
                    ExtendedSocket clientHandler = new ExtendedSocket();

                    clientHandler.socket = listener.EndAccept(ar);
                    clients.Add(clientHandler);

                    OnClientConnected(clientHandler.socket);

                    byte[] buffer = new byte[1048576];

                    clientHandler.socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveRequests), new object[] { buffer, clientHandler });
                    listener.BeginAccept(new AsyncCallback(AcceptConnection), null);
                }
                catch (Exception exc)
                {
                    OnExceptionThrown(exc.ToString());
                }
            }          
        }
        private void ReceiveResponses(IAsyncResult ar)
        {
            byte[] buffer = (byte[])((object[])ar.AsyncState)[0];
            Socket wHandler = (Socket)((object[])ar.AsyncState)[1];
            ExtendedSocket cHandler = (ExtendedSocket)((object[])ar.AsyncState)[2];

            if (ServerRunning)
            {
                try
                {
                    int bytesRead = wHandler.EndReceive(ar);
                    while (bytesRead > 0 )
                    {
                        OnResponseReceived(cHandler.socket, wHandler, null);
                        cHandler.socket.Send(buffer, 0, bytesRead, SocketFlags.None);
                        bytesRead = wHandler.Receive(buffer);
                    }

                    OnWebSocketRemoved(cHandler.socket, wHandler);

                    wHandler.Shutdown(SocketShutdown.Both);
                    wHandler.Dispose();
                    cHandler.workers.Remove(wHandler); 
                    buffer = null;

                }
                catch (Exception exc)
                {
                    OnExceptionThrown(exc.ToString());
                }
            }
        }
        private void ReceiveRequests(IAsyncResult ar)
        {
            byte[] buffer = (byte[])((object[])ar.AsyncState)[0];
            ExtendedSocket cHandler = (ExtendedSocket)((object[])ar.AsyncState)[1];

            if (ServerRunning)
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

                        String request = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                        OnRequestReceived(cHandler.socket, request);

                        if (hostParser.IsMatch(request))
                        {
                            host = hostParser.Match(request).Groups[1].Value;
                            wHandler = CreateWebSocket(host);
                        }
                        else
                        {
                            Regex hostAndPortParser = new Regex(pattern2);
                            host = hostAndPortParser.Match(request).Groups[1].Value;
                            port = int.Parse(hostAndPortParser.Match(request).Groups[2].Value);
                            wHandler = CreateWebSocket(host, port);
                        }

                        cHandler.workers.Add(wHandler);
                        OnWebSocketCreated(cHandler.socket, wHandler);

                        byte[] buffernew = new byte[1048576];

                        wHandler.Send(buffer, bytesRead, SocketFlags.None);
                        wHandler.BeginReceive(buffernew, 0, buffernew.Length, SocketFlags.None, new AsyncCallback(ReceiveResponses), new object[] { buffernew, wHandler, cHandler, 5 });

                        cHandler.socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveRequests), new object[] { buffer, cHandler });
                    }
                    else
                    {
                        while (cHandler.workers.Count != 0)
                            Thread.Sleep(10);

                        OnClientDisconnected(cHandler.socket);

                        if (cHandler.socket.Connected)
                            cHandler.socket.Shutdown(SocketShutdown.Both);

                        clients.Remove(cHandler);
                        cHandler.socket.Dispose();
                        cHandler = null;
                    }
                    
                }
                catch (Exception exc)
                {
                    OnExceptionThrown(exc.ToString());
                }
            }
        }

        //--------------//
        // Proxy events //
        //--------------//
        public event WebEvent ClientConnected;
        public event WebEvent ClientDisconnected;
        public event WebEvent RequestReceived;
        public event WebEvent ResponseReceived;
        public event WebEvent ExceptionThrown;
        public event WebEvent WebSocketCreated;
        public event WebEvent WebSocketRemoved; 

        protected virtual void OnClientConnected(Socket client)
        {
            if (ClientConnected != null)
            {
                ProxyEventArgs e = new ProxyEventArgs();
                e.sockets = new Socket[]{ client };

                ClientConnected(this, e);
            }
        }
        protected virtual void OnClientDisconnected(Socket client)
        {
            if (ClientDisconnected != null)
            {
                ProxyEventArgs e = new ProxyEventArgs();
                e.sockets = new Socket[] { client };

                ClientDisconnected(this, e);
            }
        }
        protected virtual void OnRequestReceived(Socket client, String request)
        {
            if (RequestReceived != null)
            {
                ProxyEventArgs e = new ProxyEventArgs();
                e.sockets = new Socket[] { client };
                e.EventText = request;

                RequestReceived(this, e);
            }
        }
        protected virtual void OnResponseReceived(Socket client, Socket web, String response)
        {
            if (ResponseReceived != null)
            {
                ProxyEventArgs e = new ProxyEventArgs();
                e.sockets = new Socket[] { client, web };
                e.EventText = response;

                ResponseReceived(this, e);
            }
        }
        protected virtual void OnExceptionThrown(String exception)
        {
            if (ExceptionThrown != null)
            {
                ProxyEventArgs e = new ProxyEventArgs();
                e.EventText = exception;

                ExceptionThrown(this, null);
            }
        }
        protected virtual void OnWebSocketCreated(Socket client, Socket web)
        {
            if (WebSocketCreated != null)
            {
                ProxyEventArgs e = new ProxyEventArgs();
                e.sockets = new Socket[] { client, web };

                WebSocketCreated(this, e);
            }

        }
        protected virtual void OnWebSocketRemoved(Socket client, Socket web)
        {
            if (WebSocketRemoved != null)
            {
                ProxyEventArgs e = new ProxyEventArgs();
                e.sockets = new Socket[] { client, web };

                WebSocketRemoved(this, e);
            }
        }  
    }
}
