using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Shared;
using ServerLib;

namespace GameServer
{
    public partial class ServerUI : Form
    {
        public ServerUI()
        {
            InitializeComponent();
            Load += new EventHandler(ServerUI_Load);
            this.HandleDestroyed += new EventHandler(ServerUI_HandleDestroyed);
            this.HandleCreated += new EventHandler(ServerUI_HandleCreated);
        }

        void ServerUI_HandleCreated(object sender, EventArgs e)
        {
            m_HandleDestroyed = false;
        }

        void ServerUI_HandleDestroyed(object sender, EventArgs e)
        {
            m_HandleDestroyed = true;
        }

        private bool m_HandleDestroyed = false;
        private GameLobbyServerTB m_Server = null;
        private string m_ServerName = "";

        void ServerUI_Load(object sender, EventArgs e)
        {
            Log.LogMessage += new LogMessageDelegate(Log_LogMessage);

            Log.LogMsg("Starting Game server...");

            m_Server = new Shared.GameLobbyServerTB();
            m_Server.StartServer();
            m_ServerName = ConfigHelper.GetStringConfig("ServerName");
            this.Text = "Atlas World Server -" + m_ServerName + " listening on port " + ConfigHelper.GetIntConfig("ListenOnPort");
        }

        void Log_LogMessage(string msg)
        {
            if (!m_HandleDestroyed)
            {
                Invoke(new LogMessageDelegate(AddMsgToDisplay), new object[] { msg });
            }
        }

        private List<string> MessageLog = new List<string>();

        private void AddMsgToDisplay(string msg)
        {
            msg = DateTime.Now.ToShortTimeString() + ": " + msg;

            lock (MessageLog)
            {
                lblLiveConnections.Text = ConnectionManager.PaddedConnectionCount.ToString();
                lblConnectionsInMemory.Text = GSInboundServerConnection.NUM_CONNECTIONS_IN_MEMORY.ToString();
                lblActivePlayers.Text = ConnectionManager.AuthorizedAccounts.Count.ToString();
                lblLastLoginHearbeat.Text = DateTime.Now.ToLongTimeString();
                lblLastLoginHearbeat.Text = "Disconnected!";
                lblLiveConnections.Text = ConnectionManager.PaddedConnectionCount.ToString();
                lblConnectionsInMemory.Text = GSInboundServerConnection.NUM_CONNECTIONS_IN_MEMORY.ToString();
                lblActivePlayers.Text = ConnectionManager.AuthorizedAccounts.Count.ToString();
                lblLiveConnections.Text = ConnectionManager.PaddedConnectionCount.ToString();
                lblConnectionsInMemory.Text = GSInboundServerConnection.NUM_CONNECTIONS_IN_MEMORY.ToString();
                lblActivePlayers.Text = ConnectionManager.AuthorizedAccounts.Count.ToString();
                MessageLog.Add(msg);
                while (MessageLog.Count > 250)
                {
                    MessageLog.RemoveAt(0);
                }
            }
        }


        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void ServerUI_SizeChanged(object sender, EventArgs e)
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

        private void cmdNukeAuthTickets_Click(object sender, EventArgs e)
        {
            AddMsgToDisplay("Nuked all auth tickets. " + ConnectionManager.NukeAllAuthenticationTickets() + " users nuked.");
        }

    }
}
