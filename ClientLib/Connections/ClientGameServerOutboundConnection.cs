using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace Shared
{
    /// <summary>
    ///  Represent one connection to a type of game server
    /// </summary>
    public class ClientGameServerOutboundConnection : ClientServerOutboundConnection
    {
        public ClientGameServerOutboundConnection(bool isBlocking) : base(isBlocking)
        {
            RegisterPacketHandler((int)PacketType.PacketRijndaelExchangeRequest, delegate { Client.ConnectionPhase = ClientConnectionPhase.WorldServerGreeting; });
        }

        protected override void OnSocketKilled(string msg)
        {
            base.OnSocketKilled(msg);
        }       

        protected override void OnConnected(bool success, string msg)
        {
            if (success)
            {
                Client.ConnectionPhase = ClientConnectionPhase.WorldServerConnected;
            }
            base.OnConnected(success, msg);
        }

    }
}
