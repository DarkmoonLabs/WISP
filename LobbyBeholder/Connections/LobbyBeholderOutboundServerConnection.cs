using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace Shared
{
    /// <summary>
    /// Represents one outgoing connection to another server in the Hive
    /// </summary>
    public class LobbyBeholderOutboundServerConnection : BeholderOutboundServerConnection
    {
        public LobbyBeholderOutboundServerConnection(string name, ServerBase server, string reportedIP, bool isBlocking)
            : base(name, server, reportedIP, isBlocking)
        {
        }

        protected override void OnServerLoginResponse(PacketLoginResult result)
        {
            base.OnServerLoginResponse(result);
        }

        protected override void OnConnected(bool success, string msg)
        {
            base.OnConnected(success, msg);            
        }

        protected override void OnSocketKilled(string msg)
        {
            base.OnSocketKilled(msg);
            Log1.Logger(Server.ServerUserID).Info("Delisting games for [" + Name + "].");
            DB.Instance.Lobby_UntrackGamesForServer(ServerUserID);
        }



    }
}
