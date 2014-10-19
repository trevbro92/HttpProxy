using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace HttpProxy
{
    public delegate void WebEvent(WebProxy sender);

    public class WebProxy
    {
        public WebProxy()
        {
            lastRequest = string.Empty;
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

                listener.BeginAccept(new AsyncCallback(this.AcceptConnection), listener);
            }
            catch (Exception exc)
            {
                //MessageBox.Show(exc.ToString());
            }
        }
        public void Stop()
        {
            try
            {
                if (listener.Connected)
                {
                    listener.Shutdown(SocketShutdown.Both);
                    listener.Close();
                }
            }
            catch (Exception exc)
            {
                //MessageBox.Show(exc.ToString());
            }
        }
        public string LastRequest
        {
            get { return lastRequest; }
        }

        public event WebEvent ConnectionEstablished;
        public event WebEvent ConnectionEnd;
        public event WebEvent DataReceived;

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
        protected virtual void OnDataReceived()
        {
            if (DataReceived != null)
                DataReceived(this);
        }

        private string lastRequest;
        private Socket listener;
        private IPAddress localAddress;

        //private Socket handler;

        private Socket GetProxySocket(string hostname, int portnumber = 80)
        {
            SocketPermission permission = new SocketPermission(NetworkAccess.Connect, TransportType.Tcp, "", SocketPermission.AllPorts);
            Socket proxy = new Socket(localAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            permission.Demand();
            proxy.Connect(hostname, portnumber);

            return proxy;
        }

        private void AcceptConnection(IAsyncResult ar)
        {
            try
            {
                Socket listener = (Socket)ar.AsyncState;
                Socket handler = listener.EndAccept(ar);
                OnConnectionEstablished();

                byte[] buffer = new byte[16777216];
                object[] obj = new object[2];
                obj[0] = buffer;
                obj[1] = handler;

                handler.BeginReceive(
                        buffer,                                 // An array of type bytes for received data 
                        0,                                      // Posistion in buffer to receive data  
                        buffer.Length,                          // Maximum nuber of bytes receivable (buffer size)
                        SocketFlags.None,                       // Specifies send and receive behavior
                        new AsyncCallback(ReceiveData),         // An AsyncCallback delegate 
                        obj                                     // Specifies infomation for receive operation 
                        );

                listener.BeginAccept(new AsyncCallback(AcceptConnection), listener);
            }
            catch (Exception exc)
            {
                //MessageBox.Show(exc.ToString());
            }
        }
        private void ReceiveData(IAsyncResult ar)
        {
            try
            {
                object[] obj = (object[])ar.AsyncState;
                byte[] buffer = (byte[])obj[0];
                Socket handler = (Socket)obj[1];

                int bytesRead = handler.EndReceive(ar);

                if (bytesRead > 0 && handler.LocalEndPoint.ToString() == listener.LocalEndPoint.ToString())
                {
                    String pattern1 = @"Host: ([^:]*?)\r\n";
                    String pattern2 = @"Host: (.*?):([\d]{1,5})\r\n";
                    Regex hostParser = new Regex(pattern1);
                    Socket proxy;
                    String host;
                    int port;

                    lastRequest = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    OnDataReceived();

                    if (hostParser.IsMatch(lastRequest))
                    {
                        host = hostParser.Match(lastRequest).Groups[1].Value;
                        proxy = GetProxySocket(host);
                    }
                    else
                    {
                        Regex hostAndPortParser = new Regex(pattern2);
                        host = hostAndPortParser.Match(lastRequest).Groups[1].Value;
                        port = int.Parse(hostAndPortParser.Match(lastRequest).Groups[2].Value);
                        proxy = GetProxySocket(host, port);
                    }

                    proxy.Send(buffer, bytesRead, SocketFlags.None);

                    bytesRead = proxy.Receive(buffer);
                    handler.BeginSend(buffer, 0, bytesRead, 0, new AsyncCallback(SendData), handler);

                    while (proxy.Available > 0)
                    {
                        bytesRead = proxy.Receive(buffer);
                        handler.BeginSend(buffer, 0, bytesRead, 0, new AsyncCallback(SendData), handler);
                    }

                    byte[] buffernew = new byte[16777216];
                    obj[0] = buffernew;
                    obj[1] = handler;

                    handler.BeginReceive(buffernew, 0, buffernew.Length, SocketFlags.None, new AsyncCallback(ReceiveData), obj);
                }
            }
            catch (Exception exc)
            {
                //MessageBox.Show(exc.ToString());
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
                //MessageBox.Show(exc.ToString());
            }
        }
    }
}
