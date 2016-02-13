using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO.Compression;
using System.Timers;

namespace Shared
{
    /// <summary>
    /// Represents one cluster server connecting to the server. This is a server class.
    /// </summary>
    public class GSInboundServerConnection : InboundServerConnection
    {
        public GSInboundServerConnection(Socket s, ServerBase server, bool isBlocking)
            : base(s, server, isBlocking)
        {                        
        }

        private static bool m_IsInitialized = false;
        protected override void OnInitialize()
        {
            base.OnInitialize();
            if (!m_IsInitialized)
            {
                m_IsInitialized = true;
            }
        }

        public override Guid OnPlayerConnectionRequest(ServerUser player, ref string msg)
        {
            // only allow players on the game server if we have a character selection
            if (player.CurrentCharacter == null)
            {
                return Guid.Empty;
            }

            Guid ticket = base.OnPlayerConnectionRequest(player, ref msg);
            return ticket;
        }

        /// <summary>
        /// Gets called when the central server login request gets resolved.  Return false from this method to prevent the login.
        /// Be sure to call base.OnCentralServerLoginResolved to ensure that all necessary packet handlers get registered.
        /// </summary>
        protected override bool OnClusterServerLoginResolved(PacketLoginRequest login, bool result)
        {
            if (base.OnClusterServerLoginResolved(login, result))
            {
                
            }
            return result;
        }



    }
}
