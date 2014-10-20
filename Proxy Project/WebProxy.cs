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
            clients = new List<Socket>();
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
            try
            {
                listener.Shutdown(SocketShutdown.Both);
                listener.Close();

                foreach (Socket handler in clients)
                {
                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();
                }

                clients.Clear();
            }
            catch (Exception exc)
            {
                lastException = exc.ToString();
                OnExceptionThrown();
                //continue;
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

        private string lastRequest;
        private string lastException;
        private Socket listener;
        private List<Socket> clients;
        private IPAddress localAddress;

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
            try
            {
                //Socket listener = (Socket)ar.AsyncState;
                Socket clientHandler = listener.EndAccept(ar);
                clients.Add(clientHandler);

                OnConnectionEstablished();
                
                byte[] buffer = new byte[1048576];

                clientHandler.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveRequests), new object[] {buffer, clientHandler});
                listener.BeginAccept(new AsyncCallback(AcceptConnection), null); // listener);
            }
            catch (Exception exc)
            {
                lastException = exc.ToString();
                OnExceptionThrown();
            }
        }
        private void ReceiveResponses(IAsyncResult ar)
        {
            try
            {
                byte[] buffer = (byte[])((object[])ar.AsyncState)[0];
                Socket wHandler = (Socket)((object[])ar.AsyncState)[1];
                Socket cHandler = (Socket)((object[])ar.AsyncState)[2];

                int bytesRead = wHandler.EndReceive(ar);

                while (bytesRead > 0)
                {
                    cHandler.Send(buffer, 0, bytesRead, SocketFlags.None);
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
        private void ReceiveRequests(IAsyncResult ar)
        {
            try
            {
                byte[] buffer = (byte[])((object[])ar.AsyncState)[0];
                Socket cHandler = (Socket)((object[])ar.AsyncState)[1];

                int bytesRead = cHandler.EndReceive(ar);

                if (bytesRead > 0) // && handler.LocalEndPoint.ToString() == listener.LocalEndPoint.ToString())
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
                    wHandler.BeginReceive(buffer, 0,  buffer.Length,SocketFlags.None, new AsyncCallback(ReceiveResponses), new object[] { buffer, wHandler, cHandler, 5 });

                    byte[] buffernew = new byte[1048576];
                    cHandler.BeginReceive(buffernew, 0, buffernew.Length, SocketFlags.None, new AsyncCallback(ReceiveRequests), new object[] { buffernew, cHandler });
                }
            }
            catch (Exception exc)
            {
                lastException = exc.ToString();
                OnExceptionThrown();
            }
        }
        private void SendData(IAsyncResult ar)
        {
            try
            {
                Socket handler = (Socket)ar.AsyncState;
                handler.EndSend(ar);
            }
            catch (Exception exc)
            {
                lastException = exc.ToString();
                OnExceptionThrown();
            }
        }
    }
}
