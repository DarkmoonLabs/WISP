using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace Shared
{
    /// <summary>
    /// Represents one connection initiated by a cluster game server node to elsewhere.
    /// </summary>
    public class GSOutboundConnection : OutboundServerConnection
    {
        public GSOutboundConnection(string name, ServerBase server, string reportedIP, bool isBlocking)
            : base(name, server, reportedIP, isBlocking)
        {

        }

        public override Guid OnPlayerConnectionRequest(ServerUser player, ref string msg)
        {
            // only allow players on the game server if we have a character selection
            if (player.CurrentCharacter == null)
            {
                return Guid.Empty;
            }

            return base.OnPlayerConnectionRequest(player, ref msg);
        }

    }
}
