namespace Proxy_Project
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.ButtonProxyStart = new System.Windows.Forms.Button();
            this.ButtonProxyEnd = new System.Windows.Forms.Button();
            this.PortBox = new System.Windows.Forms.NumericUpDown();
            this.PropertiesPanel = new System.Windows.Forms.Panel();
            this.DeviceLabel = new System.Windows.Forms.Label();
            this.Devices = new System.Windows.Forms.ComboBox();
            this.PortLabel = new System.Windows.Forms.Label();
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.richTextBox2 = new System.Windows.Forms.RichTextBox();
            ((System.ComponentModel.ISupportInitialize)(this.PortBox)).BeginInit();
            this.PropertiesPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // ButtonProxyStart
            // 
            this.ButtonProxyStart.Location = new System.Drawing.Point(296, 844);
            this.ButtonProxyStart.Margin = new System.Windows.Forms.Padding(4);
            this.ButtonProxyStart.Name = "ButtonProxyStart";
            this.ButtonProxyStart.Size = new System.Drawing.Size(240, 127);
            this.ButtonProxyStart.TabIndex = 0;
            this.ButtonProxyStart.Text = "Start";
            this.ButtonProxyStart.UseVisualStyleBackColor = true;
            this.ButtonProxyStart.Click += new System.EventHandler(this.ButtonProxyStart_Click);
            // 
            // ButtonProxyEnd
            // 
            this.ButtonProxyEnd.Enabled = false;
            this.ButtonProxyEnd.Location = new System.Drawing.Point(544, 844);
            this.ButtonProxyEnd.Margin = new System.Windows.Forms.Padding(4);
            this.ButtonProxyEnd.Name = "ButtonProxyEnd";
            this.ButtonProxyEnd.Size = new System.Drawing.Size(240, 127);
            this.ButtonProxyEnd.TabIndex = 1;
            this.ButtonProxyEnd.Text = "Terminate";
            this.ButtonProxyEnd.UseVisualStyleBackColor = true;
            this.ButtonProxyEnd.Click += new System.EventHandler(this.ButtonProxyEnd_Click);
            // 
            // PortBox
            // 
            this.PortBox.Location = new System.Drawing.Point(148, 13);
            this.PortBox.Margin = new System.Windows.Forms.Padding(6);
            this.PortBox.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.PortBox.Name = "PortBox";
            this.PortBox.Size = new System.Drawing.Size(106, 31);
            this.PortBox.TabIndex = 3;
            this.PortBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.PortBox.Value = new decimal(new int[] {
            8080,
            0,
            0,
            0});
            // 
            // PropertiesPanel
            // 
            this.PropertiesPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.PropertiesPanel.Controls.Add(this.DeviceLabel);
            this.PropertiesPanel.Controls.Add(this.Devices);
            this.PropertiesPanel.Controls.Add(this.PortLabel);
            this.PropertiesPanel.Controls.Add(this.PortBox);
            this.PropertiesPanel.Location = new System.Drawing.Point(16, 844);
            this.PropertiesPanel.Margin = new System.Windows.Forms.Padding(6);
            this.PropertiesPanel.Name = "PropertiesPanel";
            this.PropertiesPanel.Size = new System.Drawing.Size(262, 125);
            this.PropertiesPanel.TabIndex = 4;
            // 
            // DeviceLabel
            // 
            this.DeviceLabel.AutoSize = true;
            this.DeviceLabel.Location = new System.Drawing.Point(6, 69);
            this.DeviceLabel.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.DeviceLabel.Name = "DeviceLabel";
            this.DeviceLabel.Size = new System.Drawing.Size(37, 25);
            this.DeviceLabel.TabIndex = 7;
            this.DeviceLabel.Text = "IP:";
            // 
            // Devices
            // 
            this.Devices.FormattingEnabled = true;
            this.Devices.Location = new System.Drawing.Point(46, 63);
            this.Devices.Margin = new System.Windows.Forms.Padding(6);
            this.Devices.Name = "Devices";
            this.Devices.Size = new System.Drawing.Size(204, 33);
            this.Devices.TabIndex = 6;
            // 
            // PortLabel
            // 
            this.PortLabel.AutoSize = true;
            this.PortLabel.Location = new System.Drawing.Point(6, 17);
            this.PortLabel.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.PortLabel.Name = "PortLabel";
            this.PortLabel.Size = new System.Drawing.Size(57, 25);
            this.PortLabel.TabIndex = 5;
            this.PortLabel.Text = "Port:";
            // 
            // richTextBox1
            // 
            this.richTextBox1.Location = new System.Drawing.Point(16, 16);
            this.richTextBox1.Margin = new System.Windows.Forms.Padding(6);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.ReadOnly = true;
            this.richTextBox1.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.ForcedVertical;
            this.richTextBox1.Size = new System.Drawing.Size(768, 400);
            this.richTextBox1.TabIndex = 5;
            this.richTextBox1.Text = "";
            // 
            // richTextBox2
            // 
            this.richTextBox2.Location = new System.Drawing.Point(16, 432);
            this.richTextBox2.Margin = new System.Windows.Forms.Padding(6);
            this.richTextBox2.Name = "richTextBox2";
            this.richTextBox2.ReadOnly = true;
            this.richTextBox2.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.ForcedVertical;
            this.richTextBox2.Size = new System.Drawing.Size(768, 400);
            this.richTextBox2.TabIndex = 6;
            this.richTextBox2.Text = "";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(806, 989);
            this.Controls.Add(this.richTextBox2);
            this.Controls.Add(this.richTextBox1);
            this.Controls.Add(this.PropertiesPanel);
            this.Controls.Add(this.ButtonProxyEnd);
            this.Controls.Add(this.ButtonProxyStart);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "Form1";
            this.Text = "HTTP Proxy";
            this.ResizeEnd += new System.EventHandler(this.Form1_Resize);
            ((System.ComponentModel.ISupportInitialize)(this.PortBox)).EndInit();
            this.PropertiesPanel.ResumeLayout(false);
            this.PropertiesPanel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button ButtonProxyStart;
        private System.Windows.Forms.Button ButtonProxyEnd;
        private System.Windows.Forms.NumericUpDown PortBox;
        private System.Windows.Forms.Panel PropertiesPanel;
        private System.Windows.Forms.Label PortLabel;
        private System.Windows.Forms.ComboBox Devices;
        private System.Windows.Forms.Label DeviceLabel;
        private System.Windows.Forms.RichTextBox richTextBox1;
        private System.Windows.Forms.RichTextBox richTextBox2;

    }
}

