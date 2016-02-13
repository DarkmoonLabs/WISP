﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace Shared
{
    /// <summary>
    /// Represents an inbound connection to the central server.  If you are using a login server, then this connection
    /// represents the login server
    /// </summary>
    public class CentralLobbyInboundLoginConnection : CSInboundServerConnection
    {
        public CentralLobbyInboundLoginConnection(Socket s, ServerBase server, bool isBlocking)
            : base(s, server, isBlocking)
        {

        }

        protected override void OnParentConnectionSet()
        {
            base.OnParentConnectionSet();
            Log.LogMsg("Login Server connected and logged in.");
        }

        protected override void OnSocketKilled(string msg)
        {
            base.OnSocketKilled(msg);
            Log.LogMsg("Lost connection to " + ServerUser.AccountName + " (" + msg + ") ");
        }

        protected override void OnPing(INetworkConnection con, Packet ping)
        {
            DB.Instance.Server_Register(MyServer.ServerUserID, MyServer.ServerAddress, MyServer.ListenOnPort, DateTime.UtcNow, "lobby", ConnectionManager.PaddedConnectionCount, MyServer.MaxConnections);
            base.OnPing(con, ping);
        }

        private static bool m_HaveClearedRegs = false;
        protected override void OnConnectionReady()
        {
            base.OnConnectionReady();
            // Register in the DB as a transfer target.
            if (!m_HaveClearedRegs)
            {
                m_HaveClearedRegs = true;
                DB.Instance.Server_ClearRegistrations("lobby");
            }
            DB.Instance.Server_Register(MyServer.ServerUserID, MyServer.ServerAddress, MyServer.ListenOnPort, DateTime.UtcNow, "lobby", ConnectionManager.PaddedConnectionCount, MyServer.MaxConnections);
        }

       
    }
}
