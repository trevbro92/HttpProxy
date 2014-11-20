using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
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
            http_proxy.RequestReceived += DisplayRequest;
            http_proxy.ExceptionThrown += DisplayException;
            http_proxy.ConnectionEstablished += DisplayClients;
            http_proxy.ConnectionEnd += DisplayClients;

            var result = from ip in hostInfo.AddressList
                         where ip.AddressFamily == AddressFamily.InterNetwork
                         select ip;

            Devices.DataSource = result.Reverse().ToList();
        }

        private void DisplayRequest(HttpProxy.WebProxy sender)
        {
            if (this.InvokeRequired)
                this.Invoke((MethodInvoker)delegate() { DisplayRequest(sender); });

            else
            {
                requests_log.Add(sender.LastRequest);
                richTextBox1.Text = String.Format("Request: {0}\n", requests_log.Count) + sender.LastRequest;
                selectedRequest = requests_log.Count - 1;
            }
        }
        private void DisplayException(HttpProxy.WebProxy sender)
        {
            if (this.InvokeRequired)
                this.Invoke((MethodInvoker)delegate() { DisplayException(sender); });

            else
            {
                exceptions_log.Add(sender.LastException);
                richTextBox2.Text = String.Format("Exception: {0}\n", exceptions_log.Count) + sender.LastException;
                selectedException = exceptions_log.Count - 1;
            }
        }
        private void DisplayClients(HttpProxy.WebProxy sender)
        {
            if (this.InvokeRequired)
                this.Invoke((MethodInvoker)delegate() { DisplayClients(sender); });

            else
                this.Text = "HTTP Proxy (" + sender.ConnectedClients.ToString() + ")";
        }

        private void ButtonProxyStart_Click(object sender, EventArgs e)
        {
            http_proxy.Start((IPAddress) Devices.SelectedItem, (int) PortBox.Value);

            ButtonProxyStart.Enabled = false;
            PropertiesPanel.Enabled = false;
            ButtonProxyEnd.Enabled = true;
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
            System.Drawing.Size NewSize = this.ClientSize;
            System.Drawing.Point NewLocation = richTextBox2.Location;

            NewSize.Width -= 16;
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

            this.Refresh();
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