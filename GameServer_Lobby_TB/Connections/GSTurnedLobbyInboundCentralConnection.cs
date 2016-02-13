using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace Shared
{
    /// <summary>
    /// Represents the central lobby server connecting to this content server. This is a server class.
    /// </summary>
    public class GSTurnedLobbyInboundCentralConnection : GSLobbyInboundCentralConnection
    {
        public GSTurnedLobbyInboundCentralConnection(Socket s, ServerBase server, bool isBlocking)
            : base(s, server, isBlocking)
        {                        
        }       

        private static bool m_IsInitialized = false;
        protected override void OnInitialize()
        {
            base.OnInitialize();
            if (!m_IsInitialized)
            {
                //RegisterPacketCreationDelegate
                m_IsInitialized = true;
            }
        }      



    }
}
