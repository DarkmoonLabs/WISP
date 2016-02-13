using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Web.Security;
using System.Web.Profile;
using ServerLib;
using System.IO;

namespace Shared
{
	/// <summary>
	/// Login Server.  Handles player connections and authentication of those players against the user database (via LSInboundConnection).  Also manages
    /// player handoff to given server clusters (via LSOutboundConnection)
	/// </summary>
	public class LoginServer : ServerBase
	{
        /// <summary>
        /// The minimum number of character slots that a user has available to them.  If
        /// the Central Server's App.Config has UseCharacters=FALSE set, then this setting
        /// is not used.
        /// </summary>
        public static int MinimumCharactersAllowed { get; set; }

        /// <summary>
        /// Should login server allow the creation of new accounts via login packet flag? Setting this to false in the config will require some other kind
        /// of account creation scheme.
        /// </summary>
        public static bool AllowNewAccountsOnTheFly { get; set; }

        /// <summary>
        /// Handles receiving of new connections and creating individual socket connections for incoming login requests
        /// </summary>
		public LoginServer() : base()
		{
            MinimumCharactersAllowed = ConfigHelper.GetIntConfig("MinimumCharactersAllowed", 1);
            AllowNewAccountsOnTheFly = ConfigHelper.GetStringConfig("AllowNewAccountsOnTheFly", "FALSE").ToUpper() == "TRUE";
		}

        /// <summary>
        /// Override from ServerBase, to make sure we create the proper connection object for inbound connections.
        /// If we don't override this method, a generic InboundConnection class will be instantiated.
        /// </summary>
        protected override InboundConnection CreateInboundConnection(Socket s, ServerBase server, int serviceID, bool isBlocking)
        {
            if (serviceID == 7)
            {
                return new ZeusInboundConnection(s, server, isBlocking);
            }

            return new LSInboundConnection(s, server, isBlocking);
        }

        /// <summary>
        /// Override from ServerBase, to make sure we create the proper connection object for outgoing connections.
        /// If we don't override this method, a generic OutboundServerConnection class will be instantiated.
        /// </summary>
        public override OutboundServerConnection CreateOutboundServerConnection(string name, ServerBase server, string reportedIP, int serviceID, bool isBlocking)
        {
            return new LSOutboundConnection(name, server, reportedIP, isBlocking);
        }

    }
}
