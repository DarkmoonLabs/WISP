using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO.Compression;
using System.Timers;
using System.Data;

namespace Shared
{
    /// <summary>
    /// Represents the central lobby server connecting to the server. This is a server class.
    /// </summary>
    public class GSLobbyInboundCentralConnection : GSInboundServerConnection
    {
        public GSLobbyInboundCentralConnection(Socket s, ServerBase server, bool isBlocking)
            : base(s, server, isBlocking)
        {                        
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
            // Register in the DB as a transfer target.
            if (!m_HaveClearedRegs)
            {
                m_HaveClearedRegs = true;
                DB.Instance.Server_ClearRegistrations("content");
            }
            DB.Instance.Server_Register(MyServer.ServerUserID, MyServer.ServerAddress, MyServer.ListenOnPort, DateTime.UtcNow, "content", ConnectionManager.PaddedConnectionCount, MyServer.MaxConnections);
        }

    }
}
