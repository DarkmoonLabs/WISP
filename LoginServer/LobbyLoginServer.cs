using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared
{
    public class LobbyLoginServer : LoginServer
    {
        protected override InboundConnection CreateInboundConnection(System.Net.Sockets.Socket s, ServerBase server, int serviceID, bool isBlocking)
        {
            if (serviceID == 7)
            {
                return new ZeusInboundConnection(s, server, isBlocking);
            }

            if (serviceID == 99)
            {
                return new InboundChatLoginConnection(s, server, isBlocking);
            }

            return new InboundWorldLoginConnection(s, server, isBlocking);
        }
    }
}
