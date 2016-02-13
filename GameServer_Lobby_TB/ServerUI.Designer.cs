namespace GameServer
{
    partial class ServerUI
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ServerUI));
            this.txtLog = new System.Windows.Forms.TextBox();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.systray = new System.Windows.Forms.NotifyIcon(this.components);
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.cmdNukeAuthTickets = new System.Windows.Forms.ToolStripButton();
            this.label1 = new System.Windows.Forms.Label();
            this.lblLastLoginHearbeat = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.lblLiveConnections = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.lblConnectionsInMemory = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.lblActivePlayers = new System.Windows.Forms.Label();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.contextMenuStrip1.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // txtLog
            // 
            this.txtLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.txtLog.Location = new System.Drawing.Point(12, 108);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.Size = new System.Drawing.Size(574, 222);
            this.txtLog.TabIndex = 0;
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.exitToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(93, 26);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(92, 22);
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // systray
            // 
            this.systray.ContextMenuStrip = this.contextMenuStrip1;
            this.systray.Icon = ((System.Drawing.Icon)(resources.GetObject("systray.Icon")));
            this.systray.Text = "notifyIcon1";
            this.systray.Visible = true;
            this.systray.DoubleClick += new System.EventHandler(this.systray_DoubleClick);
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.cmdNukeAuthTickets});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(598, 25);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // cmdNukeAuthTickets
            // 
            this.cmdNukeAuthTickets.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.cmdNukeAuthTickets.Image = ((System.Drawing.Image)(resources.GetObject("cmdNukeAuthTickets.Image")));
            this.cmdNukeAuthTickets.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.cmdNukeAuthTickets.Name = "cmdNukeAuthTickets";
            this.cmdNukeAuthTickets.Size = new System.Drawing.Size(23, 22);
            this.cmdNukeAuthTickets.Text = "Nuke Tickets";
            this.cmdNukeAuthTickets.ToolTipText = "Nuke all authentication tickets";
            this.cmdNukeAuthTickets.Click += new System.EventHandler(this.cmdNukeAuthTickets_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(9, 35);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(132, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Last login server heartbeat";
            // 
            // lblLastLoginHearbeat
            // 
            this.lblLastLoginHearbeat.AutoSize = true;
            this.lblLastLoginHearbeat.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblLastLoginHearbeat.Location = new System.Drawing.Point(158, 35);
            this.lblLastLoginHearbeat.Name = "lblLastLoginHearbeat";
            this.lblLastLoginHearbeat.Size = new System.Drawing.Size(85, 13);
            this.lblLastLoginHearbeat.TabIndex = 3;
            this.lblLastLoginHearbeat.Text = "Disconnected";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(9, 58);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(91, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Live connections:";
            // 
            // lblLiveConnections
            // 
            this.lblLiveConnections.AutoSize = true;
            this.lblLiveConnections.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblLiveConnections.Location = new System.Drawing.Point(158, 58);
            this.lblLiveConnections.Name = "lblLiveConnections";
            this.lblLiveConnections.Size = new System.Drawing.Size(14, 13);
            this.lblLiveConnections.TabIndex = 5;
            this.lblLiveConnections.Text = "0";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(8, 79);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(116, 13);
            this.label3.TabIndex = 6;
            this.label3.Text = "Connections in memory";
            // 
            // lblConnectionsInMemory
            // 
            this.lblConnectionsInMemory.AutoSize = true;
            this.lblConnectionsInMemory.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblConnectionsInMemory.Location = new System.Drawing.Point(158, 79);
            this.lblConnectionsInMemory.Name = "lblConnectionsInMemory";
            this.lblConnectionsInMemory.Size = new System.Drawing.Size(14, 13);
            this.lblConnectionsInMemory.TabIndex = 7;
            this.lblConnectionsInMemory.Text = "0";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(299, 35);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(77, 13);
            this.label4.TabIndex = 8;
            this.label4.Text = "Active Players:";
            this.toolTip1.SetToolTip(this.label4, "Number of accounts currently authorized to play");
            // 
            // lblActivePlayers
            // 
            this.lblActivePlayers.AutoSize = true;
            this.lblActivePlayers.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblActivePlayers.Location = new System.Drawing.Point(422, 35);
            this.lblActivePlayers.Name = "lblActivePlayers";
            this.lblActivePlayers.Size = new System.Drawing.Size(14, 13);
            this.lblActivePlayers.TabIndex = 9;
            this.lblActivePlayers.Text = "0";
            // 
            // ServerUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(598, 342);
            this.Controls.Add(this.lblActivePlayers);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.lblConnectionsInMemory);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.lblLiveConnections);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.lblLastLoginHearbeat);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.txtLog);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "ServerUI";
            this.Text = "ServerUI";
            this.SizeChanged += new System.EventHandler(this.ServerUI_SizeChanged);
            this.contextMenuStrip1.ResumeLayout(false);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.NotifyIcon systray;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton cmdNukeAuthTickets;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label lblLastLoginHearbeat;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label lblLiveConnections;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label lblConnectionsInMemory;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Label lblActivePlayers;
    }
}