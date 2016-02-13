using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;

namespace Shared
{
	/// <summary>
	/// A central server is a hub server that sits between the login server and a game / sub-server
	/// </summary>
	public class CentralServer : ServerBase
	{
        /// <summary>
        /// Handles receiving of new connectiosns and creating individual socket connections for incoming requests
        /// </summary>
		public CentralServer() : base()
		{            
		}

        public override void StartServer()
        {
            base.StartServer();
        }

        #region boilerplate

        /// <summary>
        /// Override from ServerBase, to make sure we create the proper connection object for inbound connections.
        /// If we don't override this method, a generic CSInboundServerConnection class will be instantiated.
        /// </summary>
        protected override InboundConnection CreateInboundConnection(Socket s, ServerBase server, int serviceId, bool isBlocking)
        {
            if (serviceId == 7)
            {
                return new ZeusInboundConnection(s, server, isBlocking);
            }
            return new CSInboundServerConnection(s, server, isBlocking);
        }

        /// <summary>
        /// Override from ServerBase, to make sure we create the proper connection object for inbound connections.
        /// If we don't override this method, a generic CSOutboundConnection class will be instantiated.
        /// </summary>
        public override OutboundServerConnection CreateOutboundServerConnection(string name, ServerBase server, string reportedIP, int serviceId, bool isBlocking)
        {
            return new CSOutboundConnection(name, server, reportedIP, isBlocking);
        }

        #endregion


    }
}
