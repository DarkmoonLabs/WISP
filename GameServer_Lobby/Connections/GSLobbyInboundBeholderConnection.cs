using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace Shared
{
    /// <summary>
    /// Represents an inbound connection to the central server from the Beholder daemon.  
    /// </summary>
    class GSLobbyInboundBeholderConnection : CSInboundServerConnection
    {
        public GSLobbyInboundBeholderConnection(Socket s, ServerBase server, bool isBlocking)
            : base(s, server, isBlocking)
        {

        }

        protected override void OnParentConnectionSet()
        {
            base.OnParentConnectionSet();
            Log.LogMsg("Beholder Daemon connected and logged in.");
        }

        protected override void OnSocketKilled(string msg)
        {
            base.OnSocketKilled(msg);
            Log.LogMsg("Lost connection to Beholder Daemon " + ServerUser.AccountName + " (" + msg + ") ");
        }

        protected override void OnPing(INetworkConnection con, Packet ping)
        {
            DB.Instance.Server_Register(MyServer.ServerUserID, MyServer.ServerAddress, MyServer.ListenOnPort, DateTime.UtcNow, "content", ConnectionManager.PaddedConnectionCount, MyServer.MaxConnections);
            base.OnPing(con, ping);
        }

        private static bool m_HaveClearedRegs = false;
        protected override void OnConnectionReady()
        {
            base.OnConnectionReady();
            if (!m_HaveClearedRegs)
            {
                m_HaveClearedRegs = true;
                DB.Instance.Server_ClearRegistrations("content");
            }
            DB.Instance.Server_Register(MyServer.ServerUserID, MyServer.ServerAddress, MyServer.ListenOnPort, DateTime.UtcNow, "content", ConnectionManager.PaddedConnectionCount, MyServer.MaxConnections);
        }
       
    }
}
