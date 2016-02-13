using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace Shared
{
    public class LSOutboundConnection : OutboundServerConnection
    {
        public LSOutboundConnection(string name, ServerBase server, string reportedIP, bool isBlocking)
            : base(name, server, reportedIP, isBlocking)
        {
        }

        protected override void OnServerLoginResponse(PacketLoginResult result)
        {
            base.OnServerLoginResponse(result);
            if (result.ReplyCode == ReplyType.OK)
            {
                Log1.Logger("LoginServer.Outbound.Login").Info("Logged in successfully to " + Name);
            }
            else
            {
                Log1.Logger("LoginServer.Outbound.Login").Error("Failed to log in to " + Name + ". " + result.ReplyMessage);
            }           
        }

        protected override void OnConnected(bool success, string msg)
        {
            base.OnConnected(success, msg);
            Log1.Logger("LoginServer.Outbound.Login").Info("Connection to " + Name + " resolved - " + msg);
        }

        protected override void OnSocketKilled(string msg)
        {
            base.OnSocketKilled(msg);
            Log1.Logger("LoginServer.Outbound").Info("Lost connection to " + Name + " (" + msg + ") ");
            if (ServerUserID != null)
            {
                Log1.Logger(Server.ServerUserID).Info("Assuming [" + Name + "] is offline or unresponsive. Unregistering server from Hive as [ " + ServerUserID + "].");
                DB.Instance.Server_Unregister(ServerUserID);
            }
        }

    }
}
