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
    public class WispServiceOutboundServerConnection : OutboundServerConnection
    {
        public WispServiceOutboundServerConnection(string name, ServerBase server, string reportedIP, bool isBlocking)
            : base(name, server, reportedIP, isBlocking)
        {
        }

        /* Override any methods you like to catch various events. */

        protected override void OnServerLoginResponse(PacketLoginResult result)
        {
            base.OnServerLoginResponse(result);
            if (result.ReplyCode == ReplyType.OK)
            {
                Log1.Logger("Server").Info("Logged in successfully to " + Name);
            }
            else
            {
                Log1.Logger("Server").Info("Failed to log in to " + Name + ". " + result.ReplyMessage);
            }
        }

        protected override void OnConnected(bool success, string msg)
        {
            base.OnConnected(success, msg);
            Log1.Logger("Server").Info("Connection to " + Name + " resolved - " + msg);
        }

        protected override void OnSocketKilled(string msg)
        {
            base.OnSocketKilled(msg);
            Log1.Logger("Server").Info("Connection to " + Name + " terminated (" + msg + ") ");
        }



    }
}
