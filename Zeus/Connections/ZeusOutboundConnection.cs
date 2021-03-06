﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace Shared
{
    public class ZeusOutboundConnection : OutboundServerConnection
    {
        public ZeusOutboundConnection(string name, ServerBase server, string reportedIP, bool isBlocking)
            : base(name, server, reportedIP, isBlocking)
        {
        }

        protected override void OnServerLoginResponse(PacketLoginResult result)
        {
            base.OnServerLoginResponse(result);
            if (result.ReplyCode == ReplyType.OK)
            {
                Log1.Logger("Zeus.Outbound.Server.Login").Info("Logged in successfully to " + Name);
            }
            else
            {
                Log1.Logger("Zeus.Outbound.Server.Login").Info("Failed to log in to " + Name);
            }           
        }

        protected override void OnConnected(bool success, string msg)
        {
            base.OnConnected(success, msg);
            Log1.Logger("Zeus.Outbound.Server.Login").Info("Connection to " + Name + " resolved - " + msg);
        }

        protected override void OnSocketKilled(string msg)
        {
            base.OnSocketKilled(msg);
            Log1.Logger("Zeus.Outbound.Server").Info("Connection to " + Name + " terminated (" + msg + ") ");
        }

    }
}
