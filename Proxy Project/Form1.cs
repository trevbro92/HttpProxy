using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Linq;
using System.IO;
using HttpProxy;

namespace Proxy_Project
{
    public partial class Form1 : Form
    {
        //-------------------------//
        // Form members/properties //
        //-------------------------//
        private List<string> exceptions_log;
        private List<string> requests_log;
        private HttpProxy.WebProxy http_proxy;
        private int SelectedException { get; set; }
        private int SelectedRequest { get; set; }

        //---------------------//
        // Form event handlers //
        //---------------------//
        public Form1()
        {
            InitializeComponent();
            Form1_Resize(null, null);
            IPHostEntry hostInfo = Dns.GetHostEntry("");
            exceptions_log = new List<string>();
            requests_log = new List<string>();

            http_proxy = new HttpProxy.WebProxy();
            http_proxy.ClientConnected += AddClient;
            http_proxy.ClientDisconnected += RemoveClient;
            http_proxy.RequestReceived += DisplayRequest;
            http_proxy.ExceptionThrown += DisplayException;
            http_proxy.WebSocketCreated += AddWebHost;
            http_proxy.WebSocketRemoved += RemoveWebHost;

            var result = from ip in hostInfo.AddressList
                         where ip.AddressFamily == AddressFamily.InterNetwork
                         select ip;

            Devices.DataSource = result.Reverse().ToList();
        }
        private void Form1_Resize(object sender, EventArgs e)
        {
            System.Drawing.Size NewSize = this.ClientSize;
            System.Drawing.Point NewLocation = richTextBox2.Location;

            NewSize.Width = (int)(NewSize.Width * .6) - 16;
            NewSize.Height = (NewSize.Height - 100) / 2;
            NewLocation.Y = NewSize.Height + 16;

            richTextBox1.ClientSize = NewSize;
            richTextBox2.ClientSize = NewSize;
            richTextBox2.Location = NewLocation;

            NewLocation = PropertiesPanel.Location;
            NewLocation.Y = richTextBox2.Location.Y + NewSize.Height + 8;
            PropertiesPanel.Location = NewLocation;

            NewLocation = ButtonProxyStart.Location;
            NewLocation.Y = richTextBox2.Location.Y + NewSize.Height + 8;
            ButtonProxyStart.Location = NewLocation;

            NewLocation = ButtonProxyEnd.Location;
            NewLocation.Y = richTextBox2.Location.Y + NewSize.Height + 8;
            ButtonProxyEnd.Location = NewLocation;

            NewLocation = richTextBox1.Location;
            NewLocation.X = richTextBox1.Size.Width + 16;
            NewSize.Width = (int)(this.ClientSize.Width * .4) - 16;

            if (richTextBox2.Size.Width > 380)
                NewSize.Height = this.ClientSize.Height - 16;
            else
                NewSize.Height = this.ClientSize.Height - PropertiesPanel.Size.Height - 24;

            treeView1.Location = NewLocation;
            treeView1.Size = NewSize;

            this.Refresh();
        }

        private void ButtonProxyStart_Click(object sender, EventArgs e)
        {
            http_proxy.Start((IPAddress)Devices.SelectedItem, (int)PortBox.Value);

            ButtonProxyStart.Enabled = false;
            PropertiesPanel.Enabled = false;
            ButtonProxyEnd.Enabled = true;
            // Thread stalker = new Thread(http_proxy.)
        }
        private void ButtonProxyEnd_Click(object sender, EventArgs e)
        {
            SelectedException = 0;
            SelectedRequest = 0;

            exceptions_log.Clear();
            requests_log.Clear();

            richTextBox1.Clear();
            richTextBox2.Clear();

            foreach (TreeNode node in treeView1.Nodes)
                node.Remove();

            http_proxy.Stop();
            this.Text = "HTTP Proxy (" + http_proxy.ConnectedClients.ToString() + ")";

            ButtonProxyEnd.Enabled = false;
            ButtonProxyStart.Enabled = true;
            PropertiesPanel.Enabled = true;
        }

        private void richTextBox2_KeyDown(object sender, KeyEventArgs e)
        {
            if (exceptions_log.Count == 0)
                return;
            if (e.KeyCode == Keys.Left && SelectedException > 0)
                SelectedException--;
            else if (e.KeyCode == Keys.Right && SelectedException < exceptions_log.Count - 1)
                SelectedException++;
            else
                return;

            richTextBox2.Text = String.Format("Exception: {0}\n", SelectedException + 1) + exceptions_log[SelectedException];
            richTextBox2.SelectionStart = 0;
        }
        private void richTextBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (requests_log.Count == 0)
                return;
            if (e.KeyCode == Keys.Left && SelectedRequest > 0)
                SelectedRequest--;
            else if (e.KeyCode == Keys.Right && SelectedRequest < requests_log.Count - 1)
                SelectedRequest++;
            else
                return;

            richTextBox1.Text = String.Format("Request: {0}\n", SelectedRequest + 1) + requests_log[SelectedRequest];
            richTextBox1.SelectionStart = 0;
        }

        //----------------------//
        // Proxy event handlers //
        //----------------------//
        private void DisplayRequest(HttpProxy.WebProxy sender, ProxyEventArgs e)
        {
            if (this.InvokeRequired)
                this.Invoke((MethodInvoker)delegate() { DisplayRequest(sender, e); });

            else
            {
                requests_log.Add(e.EventText);
                richTextBox1.Text = String.Format("Request: {0}\n", requests_log.Count) + e.EventText;
                SelectedRequest = requests_log.Count - 1;
            }
        }
        private void DisplayException(HttpProxy.WebProxy sender, ProxyEventArgs e)
        {
            if (this.InvokeRequired)
                this.Invoke((MethodInvoker)delegate() { DisplayException(sender, e); });

            else
            {
                exceptions_log.Add(e.EventText);
                richTextBox2.Text = String.Format("Exception: {0}\n", exceptions_log.Count) + e.EventText;
                SelectedException = exceptions_log.Count - 1;
            }
        }

        private void AddWebHost(HttpProxy.WebProxy sender, ProxyEventArgs e)
        {
            if (this.InvokeRequired)
                this.Invoke((MethodInvoker)delegate() { AddWebHost(sender, e); });
            else try
            {
                String cid = e.sockets[0].RemoteEndPoint.ToString();
                String wid = e.sockets[1].RemoteEndPoint.ToString();  

                if (treeView1.Nodes.Find(cid, true)[0].Nodes.Find(wid, true).Length == 0)
                {
                    treeView1.BeginUpdate();
                    treeView1.Nodes.Find(cid, true)[0].Nodes.Add(wid, "Web Socket: " + wid);
                    treeView1.EndUpdate();
                }
                else if (treeView1.Nodes.Find(cid, true)[0].Nodes.Find(wid, true)[0].ForeColor == Color.Red)
                {
                    treeView1.Nodes.Find(cid, true)[0].Nodes.Find(wid, true)[0].ForeColor = Color.Black;
                }
            }
            catch (Exception exc)
            {
                treeView1.EndUpdate();
                exceptions_log.Add(exc.ToString());
                richTextBox2.Text = String.Format("Exception: {0}\n", exceptions_log.Count) + exc.ToString();
                SelectedException = exceptions_log.Count - 1;
            }
        }
        private void RemoveWebHost(HttpProxy.WebProxy sender, ProxyEventArgs e)
        {
            if (this.InvokeRequired)
                this.Invoke((MethodInvoker)delegate() { RemoveWebHost(sender, e); });
            else try
            {
                String cid = e.sockets[0].RemoteEndPoint.ToString();
                String wid = e.sockets[1].RemoteEndPoint.ToString(); 

                //treeView1.BeginUpdate();
                //treeView1.Nodes.Find(cid, true)[0].Nodes.Find(wid, true)[0].Remove();
                //treeView1.EndUpdate();

                treeView1.Nodes.Find(cid, true)[0].Nodes.Find(wid, true)[0].ForeColor = Color.Red;  // "Remove" it
            }
            catch (Exception exc)
            {
                treeView1.EndUpdate();
                exceptions_log.Add(exc.ToString());
                richTextBox2.Text = String.Format("Exception: {0}\n", exceptions_log.Count) + exc.ToString();
                SelectedException = exceptions_log.Count - 1;
            }
        }

        private void AddClient(HttpProxy.WebProxy sender, ProxyEventArgs e)
        {
            if (this.InvokeRequired)
                this.Invoke((MethodInvoker)delegate() { AddClient(sender, e); });
            else try
            {
                this.Text = "HTTP Proxy (" + sender.ConnectedClients.ToString() + ")";

                String cid = e.sockets[0].RemoteEndPoint.ToString();

                if (treeView1.Nodes.Find(cid, true).Length == 0)
                {
                    treeView1.BeginUpdate();

                    if (treeView1.Nodes.Find(cid.Split(':')[0], true).Length == 0)
                        treeView1.Nodes.Add(cid.Split(':')[0], cid.Split(':')[0]).Nodes.Add(cid, "Client socket: " + cid);
                    else
                        treeView1.Nodes.Find(cid.Split(':')[0], true)[0].Nodes.Add(cid, "Client socket: " + cid);

                    treeView1.EndUpdate();
                }
                else if (treeView1.Nodes.Find(cid, true)[0].ForeColor == Color.Red)
                {
                    treeView1.Nodes.Find(cid, true)[0].ForeColor = Color.Black;
                }
            }
            catch (Exception exc)
            {
                treeView1.EndUpdate();
                exceptions_log.Add(exc.ToString());
                richTextBox2.Text = String.Format("Exception: {0}\n", exceptions_log.Count) + exc.ToString();
                SelectedException = exceptions_log.Count - 1;
            }
        }
        private void RemoveClient(HttpProxy.WebProxy sender, ProxyEventArgs e)
        {
            if (this.InvokeRequired)
                this.Invoke((MethodInvoker)delegate() { RemoveClient(sender, e); });
            else try
            {
                this.Text = "HTTP Proxy (" + sender.ConnectedClients.ToString() + ")";

                String cid = e.sockets[0].RemoteEndPoint.ToString();

                //treeView1.BeginUpdate();
                //treeView1.Nodes.Find(cid, true)[0].Remove();
                //treeView1.EndUpdate();       

                treeView1.Nodes.Find(cid, true)[0].ForeColor = Color.Red;
            }
            catch (Exception exc)
            {
                treeView1.EndUpdate();
                exceptions_log.Add(exc.ToString());
                richTextBox2.Text = String.Format("Exception: {0}\n", exceptions_log.Count) + exc.ToString();
                SelectedException = exceptions_log.Count - 1;
            }

        } 
    } 
}