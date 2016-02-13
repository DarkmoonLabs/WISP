using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using System.Reflection;
using Shared;

namespace LobbyBeholder
{
    /// <summary>
    /// Beholder Daemon. Watches other servers in the Hive and monitors their responsiveness.
    /// </summary>
    public class BeholderServer : ServerBase
    {
        static BeholderServer()
        {
        }

        public BeholderServer()
        {
        }

        /// <summary>
        /// Override this method to create the correct connection object for connecting to another server in the Hive
        /// given the service request ID listed in the App.Config.
        /// Once the appropriate connection is created, it takes over the handshaking and communication.
        /// </summary>
        public override OutboundServerConnection CreateOutboundServerConnection(string name, ServerBase server, string reportedIP, int serviceID, bool isBlocking)
        {
            switch (serviceID)
            {
                default:
                    return new BeholderOutboundServerConnection(name, server, reportedIP, isBlocking);
            }
        }

    }
}
