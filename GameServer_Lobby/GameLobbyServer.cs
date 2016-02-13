using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Shared
{
	/// <summary>
	/// Handles room/instance/match based game communication
	/// </summary>
	public class GameLobbyServer : GameServerNode
	{
		static GameLobbyServer()
		{
			ConnectionManager.ParentDisconnected += new InboundConnection.DisconnectedDelegate(InboundConnectionManager_ParentDisconnected);
		}

        public GameLobbyServer()
            : base()
        {
            m_Instance = this;
            GlobalTaskQueue = new PFXSingleTaskQueue();
            Factory.Instance.Register(typeof(Game), delegate { return new Game(); });
            TrueSkillRating.RegisterSerializableTypes();
            DB.Instance.Lobby_UntrackGamesForServer(ServerUserID);
        }
        
		static void InboundConnectionManager_ParentDisconnected(InboundConnection con, string msg)
		{
			Log.LogMsg("Incoming server connection lost.  " + ((InboundServerConnection)con).ServerUser.AccountName + ". " + msg);
		}

		private static GameLobbyServer m_Instance;

		public static GameLobbyServer Instance
		{
			get
			{
				if (m_Instance == null)
				{
					m_Instance = new GameLobbyServer();
				}
				return m_Instance;
			}
		}

        public PFXSingleTaskQueue GlobalTaskQueue { get; set; }

        /// <summary>
        /// Saves/updates a character on a different thread
        /// </summary>
        /// <param name="completionCallback"></param>
        /// <param name="toon"></param>
        /// <param name="enforceUniqueName">You can change a toon's name in the DB. To ensure unique names, set to true. IMPORTANT!!!: If you are saving
        /// , i.e. updating an EXISTING toon without changing his name, set enforceUniqueName to FALSE - otherwise the update will fail since that 
        /// toon's name already exists in the DB, the update will fail.</param>
        public void BeginSaveCharacter(Action<Task> completionCallback, ServerCharacterInfo toon, bool enforceUniqueName)
        {
            // Save character, out of band
            Task t = new Task((state) =>
            {
                ServerCharacterInfo sci = toon as ServerCharacterInfo;
                if (toon != null)
                {
                    string msg = "";
                    if (!CharacterUtil.Instance.SaveCharacter(sci.OwningAccount, sci, enforceUniqueName, ref msg))
                    {
                        Log.LogMsg("Failed to save character [" + toon.CharacterName + " | " + toon.ID + "]. " + msg);
                    }
                }
            }, "Save Player " + toon.CharacterName, TaskCreationOptions.LongRunning);

            if (completionCallback != null)
            {
                t.ContinueWith(completionCallback, TaskContinuationOptions.AttachedToParent);
            }

            GlobalTaskQueue.AddTask(t);
        }

        /// <summary>
        /// Saves/updates a character 
        /// </summary>
        /// <param name="toon"></param>
        /// <param name="enforceUniqueName">You can change a toon's name in the DB. To ensure unique names, set to true. IMPORTANT!!!: If you are saving
        /// , i.e. updating an EXISTING toon without changing his name, set enforceUniqueName to FALSE - otherwise the update will fail since that 
        /// toon's name already exists in the DB, the update will fail.</param>
        public bool SaveCharacter(ServerCharacterInfo toon, ref string msg, bool enforceUniqueName)
        {
            return CharacterUtil.Instance.SaveCharacter(toon.OwningAccount, toon, enforceUniqueName, ref msg);
        }

		#region Boilerplate

		/// <summary>
		/// Override from ServerBase, to make sure we create the proper connection object for inbound connections.
		/// If we don't override this method, a generic InboundConnection class will be instantiated.
		/// </summary>
        protected override InboundConnection CreateInboundConnection(Socket s, ServerBase server, int serviceID, bool isBlocking)
		{
			InboundConnection con = null;
			// ServiceIDs represents the type connection that is being requested.
			// these IDs are set in the App.Config of the initiating server and are any arbitrarily agreed upon integers
			switch (serviceID)
			{
                case 7:
                    con = new ZeusInboundConnection(s, server, isBlocking);
                    con.ServiceID = serviceID;
                    break;
				case 1: // central server
					con = new GSLobbyInboundCentralConnection(s, server, isBlocking);
					con.ServiceID = serviceID;
					GameManager.Instance.CentralServer = con as GSInboundServerConnection;
					break;
                case 8: // Beholder Daemon
                    con = new GSLobbyInboundBeholderConnection(s, server, isBlocking);
                    con.ServiceID = 8;
                    break;
				default: // assume player client
					con = new GSLobbyInboundPlayerConnection(s, server, isBlocking);
					con.ServiceID = serviceID;
					break;
			}

#if DEBUG
			if (con == null)
			{
				throw new ArgumentOutOfRangeException("ServiceID " + serviceID.ToString() + " is unknown to CreateInboundConnection.  Cannot process connection.");
			}
#endif
			return con;
		}

		/// <summary>
		/// Override from ServerBase, to make sure we create the proper connection object for outbound connections.
		/// If we don't override this method, a generic OutboundServerConnection class will be instantiated.
		/// </summary>
        public override OutboundServerConnection CreateOutboundServerConnection(string name, ServerBase server, string reportedIP, int serviceID, bool isBlocking)
		{
			// we need to define a new class and instantiate it here for any outbound connections.  currently, the game lobby server
			// should have no outbound connections.  If it tries to create some (i.e. this exception has been thrown), there is likely
			// an outbound connection defined in your App.Config file that probably shouldn't be there.
			throw new NotImplementedException("No outbound connections object have been defined for GameServer_Lobby");
		}

		#endregion

	}
}
