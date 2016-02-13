using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Web.Security;
using Shared;
using ServerLib;

namespace Shared
{
    public partial class Form1 : Form
    {
        private LobbyLoginServer m_Server;

        public Form1()
        {
            InitializeComponent();
            this.Load += new EventHandler(Form1_Load);            
        }

        void Form1_Load(object sender, EventArgs e)
        {
            Log.LogMessage +=  new LogMessageDelegate(Log_LogMessage);            

            Log.LogMsg("Starting Login server...");
            m_Server = new LobbyLoginServer();
            m_Server.StartServer();

            this.Text = ConfigHelper.GetStringConfig("ServerName") + " Login Server - listening on port " + ConfigHelper.GetIntConfig("ListenOnPort");            
        }

        bool closing = false;
        protected override void OnClosing(CancelEventArgs e)
        {
            closing = true;
            base.OnClosing(e);
        }
        void Log_LogMessage(string msg)
        {
            try
            {
                if (closing) return;
                Invoke(new LogMessageDelegate(AddMsgToDisplay), new object[] { msg });
            }
            finally
            { }
        }

        private List<string> MessageLog = new List<string>();

        private void AddMsgToDisplay(string msg)
        {
            msg = DateTime.Now.ToShortTimeString() + ": " + msg;

            lock (MessageLog)
            {
                MessageLog.Add(msg);
                while (MessageLog.Count > 250)
                {
                    MessageLog.RemoveAt(0);
                }
            }

            txtLog.Clear();

            for (int i = 0; i < MessageLog.Count && i < 50; i++)
            {
                txtLog.Text += Environment.NewLine + MessageLog[MessageLog.Count -  1 - i];
                txtLog.SelectionStart = 0;
                txtLog.ScrollToCaret();
            }
            
            if (msg.Length >= 64)
            {
                systray.Text = msg.Substring(0, 59) + "...";
            }
            else
            {
                systray.Text = msg;
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.ShowInTaskbar = false;
                Hide();
            }
        }

        private void systray_DoubleClick(object sender, EventArgs e)
        {
            Show();
            this.ShowInTaskbar = true;
            this.WindowState = FormWindowState.Normal;
        }


    }
}
