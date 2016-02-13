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
    /// Represents one connection to the server. Central server is the arbiter of content on the server cluster.
    /// </summary>
    public class CSInboundServerConnection : InboundServerConnection
    {
        public CSInboundServerConnection(Socket s, ServerBase server, bool isBlocking)
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

        /// <summary>
        /// Gets called when the login attempt from the cluster server has resolved.  Return false from this method to prevent login.
        /// Be sure to call base.OnClusterServerLoginResolved to register necessary packet handler
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
