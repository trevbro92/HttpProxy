using System;
using System.Threading;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Collections.Generic;


namespace HttpProxy
{
    public delegate void WebEvent(WebProxy sender);

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

                listener.Close();
                listener = null;

                foreach (ExtendedSocket handler in clients)
                {
                    handler.State(ExtendedSocket.SocketState.Closed);

                    if(handler.socket.Connected)
                        handler.socket.Shutdown(SocketShutdown.Both);
                    handler.socket.Close(5);
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

        struct ExtendedSocket
        {
            public enum SocketState { Open, Closed };
            public Socket socket;
            private SocketState state;
            public void State(SocketState s)
            { state = s; }
            public SocketState State()
            { return state;  }
        }
        private Socket listener;
        private List<ExtendedSocket> clients;
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
                    ExtendedSocket clientHandler = new ExtendedSocket();
                    clientHandler.socket = listener.EndAccept(ar);
                    clientHandler.State(ExtendedSocket.SocketState.Open);
                    clients.Add(clientHandler);

                    OnConnectionEstablished();

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

                    while (bytesRead > 0 && cHandler.State() == ExtendedSocket.SocketState.Open)
                    {
                        cHandler.socket.Send(buffer, 0, bytesRead, SocketFlags.None);
                        bytesRead = wHandler.Receive(buffer);
                    }

                    OnResponseReceived();

                    wHandler.Shutdown(SocketShutdown.Both);
                    wHandler.Close();
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

            if (serverRunning && cHandler.State() == ExtendedSocket.SocketState.Open)
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
                        OnRequestReceived();

                        if (hostParser.IsMatch(lastRequest))
                        {
                            host = hostParser.Match(lastRequest).Groups[1].Value;
                            wHandler = CreateWebSocket(host);
                        }
                        else
                        {
                            Regex hostAndPortParser = new Regex(pattern2);
                            host = hostAndPortParser.Match(lastRequest).Groups[1].Value;
                            port = int.Parse(hostAndPortParser.Match(lastRequest).Groups[2].Value);
                            wHandler = CreateWebSocket(host, port);
                        }

                        wHandler.Send(buffer, bytesRead, SocketFlags.None);
                        wHandler.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveResponses), new object[] { buffer, wHandler, cHandler, 5 });


                        byte[] buffernew = new byte[1048576];
                        cHandler.socket.BeginReceive(buffernew, 0, buffernew.Length, SocketFlags.None, new AsyncCallback(ReceiveRequests), new object[] { buffernew, cHandler });
                    }
                }
                catch (Exception exc)
                {
                    lastException = exc.ToString();
                    OnExceptionThrown();
                }
            }
        }

        public event WebEvent ConnectionEstablished;
        public event WebEvent ConnectionEnd;
        public event WebEvent RequestReceived;
        public event WebEvent ResponseReceived;
        public event WebEvent ExceptionThrown;

        protected virtual void OnConnectionEstablished()
        {
            if (ConnectionEstablished != null)
                ConnectionEstablished(this);
        }
        protected virtual void OnConnectionEnd()
        {
            if (ConnectionEnd != null)
                ConnectionEnd(this);
        }
        protected virtual void OnRequestReceived()
        {
            if (RequestReceived != null)
                RequestReceived(this);
        }
        protected virtual void OnResponseReceived()
        {
            if (ResponseReceived != null)
                ResponseReceived(this);
        }
        protected virtual void OnExceptionThrown()
        {
            if (ExceptionThrown != null)
                ExceptionThrown(this);
        }
    }
}
