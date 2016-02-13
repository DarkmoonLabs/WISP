using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;

namespace Shared
{
	/// <summary>
	/// Central server for a lobby game type server.  I.e. private instances, game matches, etc, etc
	/// </summary>
	public class CentralLobbyServer : CentralServer
	{
		/// <summary>
		/// Handles receiving of new connections and creating individual socket connections for incoming requests
		/// </summary>
		public CentralLobbyServer()
			: base()
		{
            Factory.Instance.Register(typeof(Game), delegate { return new Game(); });
            CharacterUtil.Instance.CharacterTemplateFile = "\\Config\\Character.xml";
            //TrueSkillRating.RegisterSerializableTypes();
		}
	 
		/// <summary>
		/// Looks at all the attached content servers and determines which one has the lowest population, i.e. the one the player should play on.
        /// Returns null if no suitable servers are found (i.e. all available servers are at capacity).
		/// </summary>
		/// <returns></returns>
        public GameServerInfo<OutboundServerConnection> GetLowPopGameServer()
		{
			GameServerInfo<OutboundServerConnection> gsi = null;
			// Grab the game server with the least number of players

            GameServerInfoGroup g = OutboundServerGroups.Groups["Default"];
            if (g == null)
            {
                return null;
            }

            List<string> addresses;
            List<string> ids;
            List<int> ports;
            List<int> curConnections;
            List<int> maxConnections;

            if (!DB.Instance.Server_GetRegistrations("content", out addresses, out ids, out ports, out curConnections, out maxConnections))
            {
                gsi = g.NextConnection();
            }
            else
            {
                int low = 0;
                float lowRatio = 1f;
                for(int i = 0; i < ids.Count; i++)
                {
                    float ratio = (float)curConnections[i] / (float)maxConnections[i];
                    if (ratio < lowRatio)
                    {
                        lowRatio = ratio;
                        low = i;
                    }
                }

                if (lowRatio >= 1)
                { 
                    // All servers are at capacity
                    return null;
                }

                // See if we're connected to that server
                //gsi = GetOutboundServerByServerUserID(ids[low]);

                //if (gsi == null || !gsi.IsOnline)
                {
                    // Create a temp object with latest info from DB
                    gsi = new GameServerInfo<OutboundServerConnection>();
                    gsi.Name = ids[low];
                    gsi.UserID = ids[low];
                    gsi.IP = addresses[low];
                    gsi.Port = ports[low];
                    gsi.CurUsers = curConnections[low];
                    gsi.MaxUsers = maxConnections[low];
                }
            }

			return gsi;
		}

	
		#region Boilerplate

		/// <summary>
		/// Override from ServerBase, to make sure we create the proper connection object for inbound connections.
		/// If we don't override this method, a generic InboundConnection class will be instantiated.
		/// </summary>
        protected override InboundConnection CreateInboundConnection(Socket s, ServerBase server, int serviceID, bool isBlocking)
		{
			switch (serviceID)
			{
                case 7:
                    return new ZeusInboundConnection(s, server, isBlocking);
				case 777: // Login Server. Defined in the Login Server's App.Config
                    return new CentralLobbyInboundLoginConnection(s, server, isBlocking);
                case 8:
                    return new CentralLobbyInboundBeholderConnection(s, server, isBlocking);
				default: // assume it's a player
					return new CentralLobbyInboundPlayerConnection(s, server, isBlocking);
			}
		}

		/// <summary>
		/// Override from ServerBase, to make sure we create the proper connection object for outbound connections.
		/// If we don't override this method, a generic OutboundServerConnection class will be instantiated.
		/// </summary>
        public override OutboundServerConnection CreateOutboundServerConnection(string name, ServerBase server, string reportedIP, int serviceID, bool isBlocking)
		{
			return new CentralLobbyOutboundContentConnection(name, server, reportedIP, isBlocking);
		}

		#endregion

	}
}
