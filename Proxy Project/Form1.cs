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
        private HttpProxy.WebProxy http_proxy;
        int numExceptions;

        public Form1()
        {
            InitializeComponent();
            Form1_Resize(null, null);
            IPHostEntry hostInfo = Dns.GetHostEntry("");
            http_proxy = new HttpProxy.WebProxy();
            http_proxy.RequestReceived += DisplayRequest;
            http_proxy.ExceptionThrown += DisplayException;
            http_proxy.ConnectionEstablished += DisplayClients;
            numExceptions = 0;

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
                richTextBox1.Text = sender.LastRequest;
        }
        private void DisplayException(HttpProxy.WebProxy sender)
        {
            if (this.InvokeRequired)
                this.Invoke((MethodInvoker)delegate() { DisplayException(sender); });

            else
                richTextBox2.Text = String.Format("Exceptions: {0}\n", ++numExceptions) + sender.LastException;
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
    } 
}