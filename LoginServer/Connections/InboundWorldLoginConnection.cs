using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace Shared
{
    /// <summary>
    /// Represents one player connection coming into the Persistent State World login server.
    /// </summary>
    public class InboundWorldLoginConnection : LSInboundConnection
    {
        public InboundWorldLoginConnection(Socket s, ServerBase server, bool isBlocking)
            : base(s, server, isBlocking)
        {
        }

        protected override bool CanCreateNewAccount(Packet p, ref string msg)
        {
            ServerUser.Profile.MaxCharacters = ConfigHelper.GetIntConfig("StartingCharacterSlots", 1);
            return base.CanCreateNewAccount(p, ref msg);
        }
  
    }
}
