using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Configuration;
using ServerLib;
using Zeus;
using Microsoft.Win32;
using System.IO;

namespace Shared
{
    /// <summary>
    /// Represents one other server in the Hive connecting to this server.
    /// </summary>
    public class ZeusPatchInboundZeusConnection : ZeusInboundConnection
    {
        public ZeusPatchInboundZeusConnection(Socket s, ServerBase server, bool isBlocking)
            : base(s, server, isBlocking)
        {
            // Register Packet handlers that should be accepted for all connection, regardless if they are authenticated or not.
            // use "RegisterPacketHandler(..."
        }

        /// <summary>
        /// Override any methods you like to catch various events.  OnSocketKilled is a common one. OnClusterServerLoginResolved is also a commonly used one.
        /// </summary>
        /// <param name="msg"></param>
        protected override void OnSocketKilled(string msg)
        {
            base.OnSocketKilled(msg);
        }

        #region Performance Monitors

        private void OnPatchInfoRequest(INetworkConnection con, Packet r)
        {
            PropertyBag bag = new PropertyBag();
            bag.SetProperty("MB", ((PatchServerProcess)MyServer).MBytesPatchDataSent);
            bag.SetProperty("Num", ((PatchServerProcess)MyServer).PatchFileSent);
            bag.SetProperty("Users", ConnectionManager.ConnectionCount);

            PacketGenericMessage msg = r as PacketGenericMessage;
            r.ReplyPacket = CreateStandardReply(r, ReplyType.OK, "");
            r.ReplyPacket.Parms = bag;
        }
     
        #endregion

        protected override void LoggedInAndReady()
        {
            base.LoggedInAndReady();
            RegisterPacketHandler((int)PacketType.PacketGenericMessage, 77, OnPatchInfoRequest);
        }

    }
}
