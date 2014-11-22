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
        int users = 0;

        public Form1()
        {
            InitializeComponent();
            Form1_Resize(null, null);
            IPHostEntry hostInfo = Dns.GetHostEntry("");
            exceptions_log = new List<string>();
            requests_log = new List<string>();

            http_proxy = new HttpProxy.WebProxy();
            http_proxy.RequestReceived += DisplayRequest;
            http_proxy.ExceptionThrown += DisplayException;
            http_proxy.ConnectionEstablished += AddClient; 
            
            //http_proxy.WebSocketCreated += AddWebHost;
            /* RemoveClient is not working yet
             * http_proxy.ConnectionEnd += RemoveClient;
             */
            http_proxy.ConnectionEnd += DisplayClients;

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

        /* This should not be needed anymore, ExtendedSocket now keeps a list of WebHosts... but just in case;) */
        private void AddWebHost(HttpProxy.WebProxy sender, object[] s)
        {
            if (this.InvokeRequired)
                this.Invoke((MethodInvoker)delegate() { AddWebHost(sender, s); });
            else
            {
                HttpProxy.ExtendedSocket client = (HttpProxy.ExtendedSocket)s[0];
                Socket wHost = (Socket)s[1];

                treeView1.BeginUpdate();
                treeView1.Nodes.Add("Client " + http_proxy.ConnectedClients + ": ");
            }

        }

        /* Might just do this in WatchClients...but just in case as they say;) */
        private void AddClient(HttpProxy.WebProxy sender, string id)
        {
            if (this.InvokeRequired)
                this.Invoke((MethodInvoker)delegate() { AddClient(sender, s); });
            else
            {
                this.Text = "HTTP Proxy (" + sender.ConnectedClients.ToString() + ")";
                bool found = false;
                
                string label = @"Client " + users + @": " + id;
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

                /* this retrieves a result, not quite sure how to use it though lol
                 * var result = from TreeNode nodes in treeView1.Nodes
                                where nodes.Text == label
                                select nodes;
                
                
                
                treeView1.BeginUpdate();
                treeView1.Nodes.Add("Client " + users + ": " + tmp);
                treeView1.EndUpdate();
                 treeView1.Nodes[unique_clients].Nodes.Add(client.socket.);
                treeView1.Nodes[0].Nodes.Add("Child 2");
                treeView1.Nodes[0].Nodes[1].Nodes.Add("Grandchild");
                treeView1.Nodes[0].Nodes[1].Nodes[0].Nodes.Add("Great Grandchild");
                treeView1.EndUpdate(); 
            }
                 */
                    
            }
                
        }
        


        /** Issue: It seems you must iterate through the list of nodes in order to perform any action
         *         on a specific node :/ eff dat shit
         */
        /*private void RemoveClient(HttpProxy.WebProxy sender, object[] s)
        {
            if (this.InvokeRequired)
                this.Invoke((MethodInvoker)delegate() { RemoveClient(sender, s); });
            else
            {
                this.Text = "HTTP Proxy (" + sender.ConnectedClients.ToString() + ")";
                HttpProxy.WebProxy.ExtendedSocket client = (HttpProxy.WebProxy.ExtendedSocket)s[0];
                treeView1.BeginUpdate();
               // treeView1.Nodes.Remove(

        }*/
       /* private void DisplayClientInfo(HttpProxy.WebProxy sender, object[] s)
        {
            if (this.InvokeRequired)
                this.Invoke((MethodInvoker)delegate() { DisplayClientInfo(sender); });
            else
                this.Text = "HTTP Proxy (" + sender.ConnectedClients.ToString() + ")";
        }*/

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