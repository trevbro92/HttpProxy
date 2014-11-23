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
        private List<string> exceptions_log;
        private List<string> requests_log;
        private HttpProxy.WebProxy http_proxy;
        private int selectedException;
        private int selectedRequest;

        public Form1()
        {
            InitializeComponent();
            Form1_Resize(null, null);
            IPHostEntry hostInfo = Dns.GetHostEntry("");
            exceptions_log = new List<string>();
            requests_log = new List<string>();

            http_proxy = new HttpProxy.WebProxy();
            http_proxy.ConnectionEstablished += AddClient;
            http_proxy.ClientRemoved += RemoveClient;
            http_proxy.WebHostRemoved += RemoveWebHost;
            http_proxy.RequestReceived += DisplayRequest;
            http_proxy.WebSocketCreated += AddWebHost;
            http_proxy.ExceptionThrown += DisplayException;
            //http_proxy.ConnectionEnd += ResetForm;

            var result = from ip in hostInfo.AddressList
                         where ip.AddressFamily == AddressFamily.InterNetwork
                         select ip;

            Devices.DataSource = result.Reverse().ToList();

        }

        private void DisplayRequest(HttpProxy.WebProxy sender, object[] s)
        {
            if (this.InvokeRequired)
                this.Invoke((MethodInvoker)delegate() { DisplayRequest(sender, new object[] {}); });

            else
            {
                requests_log.Add(sender.LastRequest);
                richTextBox1.Text = String.Format("Request: {0}\n", requests_log.Count) + sender.LastRequest;
                selectedRequest = requests_log.Count - 1;
            }
        }
        private void DisplayException(HttpProxy.WebProxy sender, object[] s)
        {
            if (this.InvokeRequired)
                this.Invoke((MethodInvoker)delegate() { DisplayException(sender, new object[] { }); });

            else
            {
                exceptions_log.Add(sender.LastException);
                richTextBox2.Text = String.Format("Exception: {0}\n", exceptions_log.Count) + sender.LastException;
                selectedException = exceptions_log.Count - 1;
            }
        }

        private void AddWebHost(HttpProxy.WebProxy sender, object[] info)
        {
            if (this.InvokeRequired)
                this.Invoke((MethodInvoker)delegate() { AddWebHost(sender, info); });
            else
            {
                String cid = (String)info[0];
                Socket wHost = (Socket)info[1];
                String wid = wHost.RemoteEndPoint.ToString().Split(':')[0];

                
                foreach (TreeNode node in treeView1.Nodes)
                {
                    if(node.Text == cid)
                    {
                        treeView1.BeginUpdate();
                        node.Nodes.Add("Web Server: " +wid);
                        treeView1.EndUpdate();
                        break;
                    }
                }
                
            }

        }
        private void RemoveWebHost(HttpProxy.WebProxy sender, object[] info)
        {
            if (this.InvokeRequired)
                this.Invoke((MethodInvoker)delegate() { RemoveWebHost(sender, info); });
            else
            {
                String cid = (String)info[0];
                String wid = (String)info[1];

                foreach (TreeNode client in treeView1.Nodes)
                {
                    if (client.Text == cid)
                    {
                        foreach(TreeNode host in client.Nodes)
                        {
                            if(host.Text == wid)
                            {
                                treeView1.BeginUpdate();
                                host.Nodes.Remove(host);
                                treeView1.EndUpdate();
                                return;
                            }
                            
                        }
                        
                    }
                }


            }

        }
        
        private void AddClient(HttpProxy.WebProxy sender, object[] info)
        {
            if (this.InvokeRequired)
                this.Invoke((MethodInvoker)delegate() { AddClient(sender, info); });
            else
            {
                this.Text = "HTTP Proxy (" + sender.ConnectedClients.ToString() + ")";
                bool found = false;

                String label = (string)info[0];  
                foreach (TreeNode node in treeView1.Nodes)
                {
                    if (node.Text == label)
                        found = true;
                }
                if (!found)
                {
                    treeView1.BeginUpdate();
                    treeView1.Nodes.Add(label);
                    treeView1.EndUpdate();
                }
                    
            }
                
        }
        private void RemoveClient(HttpProxy.WebProxy sender, object[] info)
        {
            if (this.InvokeRequired)
                this.Invoke((MethodInvoker)delegate() { RemoveClient(sender, info); });
            else
            {
                this.Text = "HTTP Proxy (" + sender.ConnectedClients.ToString() + ")";
                String client_id = (String)info[0];
                treeView1.BeginUpdate();
                foreach (TreeNode node in treeView1.Nodes)
                {
                    if(node != null && node.Text == client_id)
                        treeView1.Nodes.Remove(node);
                }
            }

        }

        private void ButtonProxyStart_Click(object sender, EventArgs e)
        {
            http_proxy.Start((IPAddress) Devices.SelectedItem, (int) PortBox.Value);

            ButtonProxyStart.Enabled = false;
            PropertiesPanel.Enabled = false;
            ButtonProxyEnd.Enabled = true;
            // Thread stalker = new Thread(http_proxy.)
        }
        private void ButtonProxyEnd_Click(object sender, EventArgs e)
        {
            selectedException = 0;
            selectedRequest = 0;

            exceptions_log.Clear();
            requests_log.Clear();

            richTextBox1.Clear();
            richTextBox2.Clear();

            foreach (TreeNode node in treeView1.Nodes)
                treeView1.Nodes.Clear();

            http_proxy.Stop();

            ButtonProxyEnd.Enabled = false;
            ButtonProxyStart.Enabled = true;
            PropertiesPanel.Enabled = true;
        }
        private void Form1_Resize(object sender, EventArgs e)
        {

        }
        private void Form1_Load(object sender, EventArgs e)
        {

        }
        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void richTextBox2_KeyDown(object sender, KeyEventArgs e)
        {
            if (exceptions_log.Count == 0)
                return;

            if (e.KeyCode == Keys.Left && selectedException > 0)
                selectedException--;

            else if (e.KeyCode == Keys.Right && selectedException < exceptions_log.Count-1)
                selectedException++;

            richTextBox2.Text = String.Format("Exception: {0}\n", selectedException + 1) + exceptions_log[selectedException];
            richTextBox2.SelectionStart = 0;
        }
        private void richTextBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (requests_log.Count == 0)
                return;

            if (e.KeyCode == Keys.Left && selectedRequest > 0)
                selectedRequest--;

            else if (e.KeyCode == Keys.Right && selectedRequest < requests_log.Count - 1)
                selectedRequest++;

            richTextBox1.Text = String.Format("Request: {0}\n", selectedRequest + 1) + requests_log[selectedRequest];
            richTextBox1.SelectionStart = 0;
        }
    } 
}