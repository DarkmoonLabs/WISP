using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Data.SqlClient;
using System.Data;

namespace Shared
{
    /// <summary>
    /// Represents an inbound connection to the central server.  If you are using a login server, then this connection
    /// represents the login server
    /// </summary>
    public class CentralLobbyInboundPlayerConnection : CSInboundPlayerConnection
    {

        public CentralLobbyInboundPlayerConnection(Socket s, ServerBase server, bool isBlocking)
            : base(s, server, isBlocking)
        {
        }

        /// <summary>
        /// Get a game server for creating a new game on.
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        protected GameServerInfo<OutboundServerConnection> RequestCreateNewGameServer(out string msg)
        {
            //Monitor.Enter(GameDataLock);
            msg = "";
            try
            {
                // Send request to server
                GameServerInfo<OutboundServerConnection> gsi = ((CentralLobbyServer)MyServer).GetLowPopGameServer();
                if (gsi == null)
                {
                    msg = "No server is available to host content on at this time. Try again in a little while.";
                    return null;
                }

                return gsi;
            }
            catch (Exception e)
            {
                int x = 0;
            }
            finally
            {
                //Monitor.Exit(GameDataLock);
            }
            return null;
        }       

        private static bool m_IsInitialized = false;
        protected override void OnInitialize()
        {
            base.OnInitialize();
            if (!m_IsInitialized)
            {
                RegisterPacketCreationDelegate((int)LobbyPacketType.MatchNotification, delegate { return new PacketMatchNotification(); });
                RegisterPacketCreationDelegate((int)LobbyPacketType.MatchRefresh, delegate { return new PacketMatchRefresh(); });
                RegisterPacketCreationDelegate((int)LobbyPacketType.QuickMatchResult, delegate { return new PacketQuickMatchResult(); });  
                m_IsInitialized = true;
            }            
        }

        protected override bool OnCharacterLoading(SqlConnection con, SqlTransaction tran, ServerCharacterInfo ci)
        {
            // Attach TrueSkill component
            if (ci.GetComponent<TSCharacterComponent>() == null)
            {
                TSCharacterComponent trueSkill = new TSCharacterComponent();
                ci.AddComponent(trueSkill);
            }

            bool rslt = CharacterUtil.Instance.LoadCharacter_TSRating(ci, con, tran);

            if (rslt)
            {
                return base.OnCharacterLoading(con, tran, ci);
            }

            return false;
        }

        protected override bool OnCharacterDeleting(SqlConnection con, SqlTransaction tran, int characterId, ServerUser owner)
        {
            bool rslt = CharacterUtil.Instance.DeleteCharacter_TSRating(characterId, con, tran);

            if (rslt)
            {
                return base.OnCharacterDeleting(con, tran, characterId, owner);
            }

            return false;
        }

        protected override bool OnCharacterPersisting(SqlConnection con, SqlTransaction tran, ServerCharacterInfo ci)
        {
            // TrueSkill component
            if (ci.GetComponent<TSCharacterComponent>() == null)
            {
                TSCharacterComponent trueSkill = new TSCharacterComponent();
                ci.AddComponent(trueSkill);
            }

            bool rslt = CharacterUtil.Instance.PersistNewCharacter_TSRating(ci, con, tran);
            if (rslt)
            {
                return base.OnCharacterPersisting(con, tran, ci);
            }

            return false;
        }

        protected override bool OnCharacterSaving(SqlConnection con, SqlTransaction tran, ServerCharacterInfo ci)
        {
            bool rslt = CharacterUtil.Instance.SaveCharacter_TSRating(ci, con, tran);

            if (rslt)
            {
                return base.OnCharacterSaving(con, tran, ci);
            }

            return false;
        }

        private void OnGameListingRequest(INetworkConnection con, Packet gmsg)
        {
            PacketMatchRefresh note = (PacketMatchRefresh)CreatePacket((int)LobbyPacketType.MatchRefresh, 0, false, false);
            
            note.IsServerPacket = false;
            note.IsRefresh = true;
            note.ReplyCode = ReplyType.OK;
            note.Kinds.Add(MatchNotificationType.ListingRefresh);
            note.TargetPlayers.Add(null);
            gmsg.ReplyPacket = note;

            if (!ValidateHasCurrentCharacter())
            {
                note.ReplyMessage = "You must select a character before you can receive a content listing.";
                note.ReplyCode = ReplyType.Failure;
                return;
            }

            PacketGenericMessage gp = gmsg as PacketGenericMessage;
            int page = gp.Parms.GetIntProperty("Page").GetValueOrDefault(0);
            int numPerPage = gp.Parms.GetIntProperty("NumPerPage").GetValueOrDefault(50);
            bool includeInProgress = gp.Parms.GetBoolProperty("IncludeInProgress").GetValueOrDefault(true);
            int minPlayersAllowed = gp.Parms.GetIntProperty("MinPlayersAllowed").GetValueOrDefault(0);

            string msg = "";
            DataTable dt = DB.Instance.Lobby_GetGameListing(page, numPerPage, "", includeInProgress, minPlayersAllowed, gp.Parms, out msg);
            bool gotRowCount = false;
            if (dt != null)  
            { // Transform data result into game object
                foreach (DataRow r in dt.Rows)
                {
                    if (!gotRowCount)
                    {
                        gotRowCount = true;
                        note.Parms.SetProperty("TotalGames", (int)r["TotalRows"]);
                    }
                    Game g = new Game();
                    g.GameID = (Guid)r["GameID"];
                    g.Started = (bool)r["InProgress"];
                    g.MaxPlayers = (int)r["MaxPlayersAllowed"];
                    g.Name = (string)r["GameName"];
                    g.Properties.SetProperty("IsPrivate", (bool)r["IsPrivate"]);
                    g.Properties.SetProperty("PlayerCount", (int)r["CurrentPlayers"]);
                    // allow custom searches to add their additional data to the game object
                    OnGameSearchResultTransform(g, r);
                    note.TheGames.Add(g);
                }
            }            
        }

        private void OnQuickMatchRequest(INetworkConnection con, Packet gmsg)
        {
            PacketQuickMatchResult note = (PacketQuickMatchResult)CreatePacket((int)LobbyPacketType.QuickMatchResult, 0, false, false);
            gmsg.ReplyPacket = note;

            if (!ValidateHasCurrentCharacter())
            {
                note.ReplyMessage = "You must select a character before you can get a quick match .";
                note.ReplyCode = ReplyType.Failure;
                return;
            }

            PacketGenericMessage gp = gmsg as PacketGenericMessage;
           
            string msg = "";
            gp.Parms.SetProperty("QuickMatch", true);
            DataTable dt = DB.Instance.Lobby_GetQuickMatch(gp.Parms, out msg);
            bool gotRowCount = false;
            if (dt != null)
            { // Transform data result into game object
                foreach (DataRow r in dt.Rows)
                {
                    note.ReplyCode = ReplyType.OK;
                    Game g = new Game();
                    g.GameID = (Guid)r["GameID"];
                    g.Started = (bool)r["InProgress"];
                    g.MaxPlayers = (int)r["MaxPlayersAllowed"];
                    g.Name = (string)r["GameName"];
                    g.Properties.SetProperty("IsPrivate", (bool)r["IsPrivate"]);
                    g.Properties.SetProperty("PlayerCount", (int)r["CurrentPlayers"]);
                    // allow custom searches to add their additional data to the game object
                    OnQuickMatchGameSearchResultTransform(g, r);
                    note.TheGame = g;
                }
            }
        }

        /// <summary>
        /// Override to add custom game search data to the Game object for player game search.
        /// </summary>
        /// <param name="g">the game object as sent to the player</param>
        /// <param name="r">The data row containing all the data for this game</param>
        protected virtual void OnQuickMatchGameSearchResultTransform(Game g, DataRow r)
        {
        }


        /// <summary>
        /// Override to add custom game search data to the Game object for player game search.
        /// </summary>
        /// <param name="g">the game object as sent to the player</param>
        /// <param name="r">The data row containing all the data for this game</param>
        protected virtual void OnGameSearchResultTransform(Game g, DataRow r)
        {
        }

        private void OnCreateNewGame(INetworkConnection con, Packet gmsg)
        {
            PacketGenericMessage genMsg = gmsg as PacketGenericMessage;
            PacketMatchNotification note = (PacketMatchNotification)CreatePacket((int)LobbyPacketType.MatchNotification, 0, false, false);
            note.Kind = MatchNotificationType.MatchCreated;
            note.NeedsReply = false;

            if (!ValidateHasCurrentCharacter())
            {
                genMsg.ReplyPacket = note;
                note.ReplyMessage = "You must select a character before you can create a new game.";
                note.ReplyCode = ReplyType.Failure;
                return;
            }

            // 
            // <h2 style="text-align: center"><span style="color: #993300;"><a href="http://www.survivalnotes.org/content/category/fire"><span style="color: #993300;">Latest Entries</span></a>           <a href="http://www.survivalnotes.org/content/category/fire?r_sortby=highest_rated&amp;r_orderby=desc"><span style="color: #993300;">Highest Rated Entries</span></a></span></h2> 
            // <h2 style="text-align: center"><span style="color: #993300;"><a href="http://www.survivalnotes.org/content/category/fire"><span style="color: #993300;">Latest Entries</span></a>           &nbsp;&nbsp;&nbsp;&nbsp;             <a href="http://www.survivalnotes.org/content/category/fire?r_sortby=highest_rated&amp;r_orderby=desc"><span style="color: #993300;">Highest Rated Entries</span></a></span></h2>
            // 
           
            Log1.Logger(MyServer.ServerUserID).Info(string.Format("Player {0} requesting create new game '{1}' ...", ServerUser.AccountName, genMsg.Parms.GetStringProperty((int)PropertyID.Name)));
            string msg = "";

            GameServerInfo<OutboundServerConnection> gsi = RequestCreateNewGameServer(out msg);
            genMsg.ReplyPacket = note;
            note.ReplyMessage = msg;
            if (gsi == null)
            {
                // Failed. Either no servers online, or they are full.
                note.ReplyCode = ReplyType.Failure;
                return;
            }
            else
            {
                // Contact that server and request a game be created.

                note.ReplyCode = ReplyType.OK;
                ServerUser.TransferToServerUnassisted(gsi.IP, gsi.Port, Guid.Empty, gsi.Name,  gsi.UserID);
            }
        }

        private void OnPlayerJoinGame(INetworkConnection con, Packet gmsg)
        {
            PacketGenericMessage genMsg = gmsg as PacketGenericMessage;
            if (!ValidateHasCurrentCharacter())
            {
                return;
            }

            Guid gameId = genMsg.Parms.GetGuidProperty((int)PropertyID.GameId);
            ServerCharacterInfo ci = ServerUser.CurrentCharacter;

            string address = "";
            int port = 0;
            string name = "";
            Log1.Logger(MyServer.ServerUserID).Info(string.Format("Player {0} requesting join content '{1}'...", ServerUser.AccountName, gameId.ToString()));

            if (DB.Instance.Lobby_GetGameServer(gameId, out address, out port, out name) && address.Length > 0 && port > 0)
            {
                Log.LogMsg(string.Format("Transferring Player {0} to join content '{1}' on server @ {2} ...", ServerUser.AccountName, gameId.ToString(), address));

                PacketMatchNotification note = (PacketMatchNotification)CreatePacket((int)LobbyPacketType.MatchNotification, 0, false, false);
                note.Kind = MatchNotificationType.PlayerAdded;
                note.TargetPlayer = ci.CharacterInfo;
                note.Parms = genMsg.Parms;
                note.ReplyCode = ReplyType.OK;
                Send(note);

                ServerUser.TransferToServerUnassisted(address, port, gameId, name, name);
            }
            else
            {
                Log.LogMsg(string.Format("Player {0} failed to join content '{1}' : {2} ...", ServerUser.AccountName, gameId.ToString(), "Couldn't find game. Join canceled."));
                PacketMatchNotification note = (PacketMatchNotification)CreatePacket((int)LobbyPacketType.MatchNotification, 0, false, false);
                note.ReplyMessage = "Game couldn't be found. Strange...";
                note.Kind = MatchNotificationType.PlayerAdded;
                note.TargetPlayer = ci.CharacterInfo;
                note.Parms = genMsg.Parms;
                note.ReplyCode = ReplyType.Failure;
                genMsg.ReplyPacket = note;
                return;
            }
        }
        
        protected override bool OnPlayerLoginResolved(PacketLoginRequest login, bool result, ref string msg)
        {
            // register packet handlers if we're logged in, i.e. authenticated - so that we will start listening for those packets.
            if (result && base.OnPlayerLoginResolved(login, result, ref msg))
            {
                RegisterPacketHandler((int)PacketType.PacketGenericMessage, (int)GenericLobbyMessageType.CreateGame, OnCreateNewGame);
                RegisterPacketHandler((int)PacketType.PacketGenericMessage, (int)GenericLobbyMessageType.JoinGame, OnPlayerJoinGame);
                RegisterPacketHandler((int)PacketType.PacketGenericMessage, (int)GenericLobbyMessageType.RequestGameListing, OnGameListingRequest);
                RegisterPacketHandler((int)PacketType.PacketGenericMessage, (int)GenericLobbyMessageType.RequestQuickMatch, OnQuickMatchRequest);
            }

            return result;
        }       

        protected override void OnSocketKilled(string msg)
        {
            base.OnSocketKilled(msg);
            DB.Instance.Server_Register(MyServer.ServerUserID, MyServer.ServerAddress, MyServer.ListenOnPort, DateTime.UtcNow, "lobby", ConnectionManager.PaddedConnectionCount, MyServer.MaxConnections);
        }
    }
}
