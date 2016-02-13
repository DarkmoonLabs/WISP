using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace Shared
{
    /// <summary>
    /// Handles a single player connection to the lobby game server
    /// </summary>
    public class GSLobbyInboundPlayerConnection : GSInboundPlayerConnection
    {
        public GSLobbyInboundPlayerConnection(Socket s, ServerBase server, bool isBLocking)
            : base(s, server, isBLocking)
        {            
        }      

        protected virtual bool OnPlayerRequestStartGame(PacketGenericMessage msg, ref string info)
        {            
            return true;
        }

        public void OnPlayerLeaveGame(INetworkConnection sender, Packet gmsg)
        {
            PacketGenericMessage msg = gmsg as PacketGenericMessage;
            GameServerGame g = ServerUser.CurrentCharacter.GetCurrentGame();
            if (g != null)
            {
                g.RemovePlayer(ServerUser.CurrentCharacter, "left the game.", true);
                g.RemoveObserver(ServerUser.CurrentCharacter);
            }
        }

        public void OnRequestStartGame(INetworkConnection sender, Packet gmsg)
        {
            PacketGenericMessage msg = gmsg as PacketGenericMessage;
            msg.ReplyPacket = CreateStandardReply(msg, ReplyType.Failure, "");
            try
            {
                GameServerGame game = ServerUser.CurrentCharacter.GetCurrentGame();
                if (!game.IsPlayerPartOfGame(ServerUser.CurrentCharacter.ID))
                {
                    KillConnection("Tried to send game packets to game that we're not part of any more.");
                    return; ;
                }

                if (!game.GameStartStrategy.CanGameBeStartedManually)
                {
                    game.SendGameInfoMessageToPlayer(ServerUser.CurrentCharacter, "Game will be started automatically when ready.");
                    return;
                }

                if (!game.GameStartStrategy.DoesGameMeetStartConditions)
                {
                    game.SendGameInfoMessageToPlayer(ServerUser.CurrentCharacter, "Game can't be started yet.");
                    return;
                }

                if (game.Owner != ServerUser.CurrentCharacter.ID)
                {
                    // send a nag message
                    game.BroadcastGameInfoMessage(ServerUser.CurrentCharacter.CharacterName + " is requesting that we begin. Leader, please start game if ready.", true);
                    return;
                }

                string tmsg = "";
                if (!OnPlayerRequestStartGame(msg, ref tmsg) || !game.StartGame(ref tmsg, true))
                {
                    msg.ReplyPacket.ReplyMessage = tmsg;
                    return;
                }

                // if it succeeded, no need to send the reply packet as everyone will have received a game started message from game.StartGame()
                msg.NeedsReply = false;
            }
            catch (Exception e)
            {
                msg.ReplyPacket.ReplyMessage = "Unknown error occured OnRequestStartGame.";
                Log.LogMsg("[" + ServerUser.AccountName + "] failed to start game. " + e.Message);
            }
        }


        /// <summary>
        /// Queries the hive to see which lobby (central) server we can bounce the player to.
        /// Returns false if all lobby servers claim to be at capacity.  At that point, just D/C the player.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public static bool GetCentralHandoffAddress(ref string address, ref int port, ref string targetId)
        {
            GameServerInfo<OutboundServerConnection> gsi = null;
            // Grab the game server with the least number of players

            List<string> addresses;
            List<string> ids;
            List<int> ports;
            List<int> curConnections;
            List<int> maxConnections;
            int low = -1;

            if (!DB.Instance.Server_GetRegistrations("lobby", out addresses, out ids, out ports, out curConnections, out maxConnections))
            {
                // no lobby servers available
                return false;
            }
            else
            {
                low = 0;
                float lowRatio = 1f;
                for (int i = 0; i < ids.Count; i++)
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
                    return false;
                }
            }

            address = addresses[low];
            port = ports[low];
            targetId = ids[low];
            return true;
        }

        /// <summary>
        /// Transfers this connection to an available lobby server or, if there is no lobby server available, disconnects the player.
        /// </summary>
        public void TransferToLobbyOrDisconnect()
        {
            string address = "";
            int port = 0;
            string serverId = "";
            if (!GetCentralHandoffAddress(ref address, ref port, ref serverId))
            {
                KillConnection("Unable to host player on this server.  No lobby server found to hand off too.");
                return;
            }

            ServerUser.TransferToServerUnassisted(address, port, Guid.Empty, "Lobby Server", serverId);
        }
                
        protected override bool OnPlayerLoginResolved(PacketLoginRequest login, bool result, ref string msg)
        {   
            ///.............
            if (!base.OnPlayerLoginResolved(login, result, ref msg))
            {
                return result;
            }

            if(ServerUser.CurrentCharacter == null)
            {
                msg = "Can't join or create games without an active character.";
                return false;
            }

            Guid gameId = Guid.Empty;
            GameServerGame sg = null;
            PacketMatchNotification note = (PacketMatchNotification)CreatePacket((int)LobbyPacketType.MatchNotification, 0, false, false);
            
            // Requesting to join as observer?
            bool observeOnly = login.Parms.GetBoolProperty("Observe").GetValueOrDefault(false);

            if (login.Parms.GetBoolProperty("IsNewGame").GetValueOrDefault(false))
            {
                Log1.Logger("Server").Debug("Player [" + ServerUser.AccountName + "] logging in with NEW GAME request.");
                note.Kind = MatchNotificationType.MatchCreated;
                note.NeedsReply = false;
                note.ReplyCode = ReplyType.Failure;

                // new game. create it.
                Game g = new Game();
                g.Name = login.Parms.GetStringProperty((int)PropertyID.Name);
                g.MaxPlayers = login.Parms.GetIntProperty((int)PropertyID.MaxPlayers).GetValueOrDefault(1);
                g.MaxObservers = login.Parms.GetIntProperty((int)PropertyID.MaxObservers).GetValueOrDefault(0);
                g.Owner = ServerUser.CurrentCharacter.ID;                
                
                // Create the actual game object, specific to the game type we are serving.
                sg = OnCreateNewGameServerGame(g);
                //sg = null;

                if (sg != null)
                {
                    // Track the new game object on this server
                    if (!GameManager.Instance.CreateNewGame(sg, true))
                    {
                        msg = "Failed to create game on server.  Internal server error.";
                        note.ReplyMessage = msg;
                        Send(note);
                        TransferToLobbyOrDisconnect();
                        return true;
                    }

                    // Report the new game in the database
                    if (!DB.Instance.Lobby_TrackGameForServer(MyServer.ServerUserID, sg, DateTime.UtcNow, ServerUser))
                    {
                        msg = "Failed to register the game in the database.  Internal server error.";
                        note.ReplyMessage = msg;
                        Send(note);
                        TransferToLobbyOrDisconnect();
                        return true;
                    }

                    // Update player counts for this server, based on the expected player count for the new game.
                    if (DB.Instance.Server_Register(MyServer.ServerUserID, MyServer.ServerAddress, MyServer.ListenOnPort, DateTime.UtcNow, "content", ConnectionManager.PaddedConnectionCount, MyServer.MaxConnections))
                    {
                        note.ReplyCode = ReplyType.OK;
                    }
                    else
                    {
                        DB.Instance.Lobby_UntrackGameForServer(sg.GameID);
                        note.ReplyMessage = "Failed to register game with master game listing. Internal server error.";
                        Send(note);
                        TransferToLobbyOrDisconnect();
                        return true;
                    }
                }
                else
                {
                    msg = "Unable to create game at this time.";
                    Log1.Logger("Server").Debug("Failed to create NEW GAME for player [" + ServerUser.AccountName + "].");
                    login.ReplyPacket.Parms.SetProperty("CreateReply", msg);
                    note.ReplyMessage = msg;
                    Send(note);
                    TransferToLobbyOrDisconnect();
                    return true;
                }

            }
            else // it's a join - get game reference from existing games table
            {                
                Log1.Logger("Server").Debug("Player [" + ServerUser.AccountName + "] logging in with request to JOIN EXISTING game. [Observe = " + observeOnly.ToString() + "]");
                note = (PacketMatchNotification)CreatePacket((int)LobbyPacketType.MatchNotification, 0, false, false);

                if (observeOnly)
                {
                    note.Kind = MatchNotificationType.ObserverAdded;
                }
                else
                {
                    note.Kind = MatchNotificationType.PlayerAdded;
                }

                note.TargetPlayer = ServerUser.CurrentCharacter.CharacterInfo;
                note.ReplyCode = ReplyType.Failure;

                Guid targetGame = login.Parms.GetGuidProperty((int)PropertyID.GameId);
                if (targetGame == Guid.Empty)
                {
                    msg = "Failed to join game. No target game specified.";
                    Log1.Logger("Server").Debug("Player [" + ServerUser.AccountName + "] failed to JOIN EXISTING game.  No target game specified in join request.");
                    note.ReplyMessage = msg;
                    Send(note);
                    TransferToLobbyOrDisconnect();
                    return true;
                }

                if (!GameManager.Instance.GetGame(targetGame, out sg))
                {
                    Log1.Logger("Server").Debug("Player [" + ServerUser.AccountName + "] failed to JOIN EXISTING game.  Target game specified does not currently exist.");
                    msg = "Failed to join game. Target game [" + targetGame.ToString() + "] does not currently exist on [" + MyServer.ServerUserID + "].";
                    note.ReplyMessage = msg;
                    Send(note);
                    TransferToLobbyOrDisconnect();
                    return true;
                }                
            }

            if (sg == null)
            {
                Log1.Logger("Server").Debug("Player [" + ServerUser.AccountName + "] failed to locate game. Internal Server Error.");
                msg = "Unable to locate game. Internal Server Error."; // should never happen
                note.ReplyMessage = msg;
                Send(note);
                TransferToLobbyOrDisconnect();                
                return true;
            }

            // join game as player or observer
            if (!observeOnly && sg.AddPlayer(ServerUser.CurrentCharacter, ref msg))
            {
                login.ReplyPacket.Parms.SetProperty("TargetGame", (ISerializableWispObject)sg.Game);
                RegisterPacketHandler((int)PacketType.PacketGenericMessage, (int)GenericLobbyMessageType.RequestStartGame, OnRequestStartGame);
            }
            else if (observeOnly && sg.AddObserver(ServerUser.CurrentCharacter, ref msg))
            {
                login.ReplyPacket.Parms.SetProperty("TargetGame", (ISerializableWispObject)sg.Game);                
            }
            else
            {
                msg = "Unable to join game.";
                note.ReplyMessage = msg;
                Send(note);
                TransferToLobbyOrDisconnect();
                return true;
            }
                        
            RegisterPacketHandler((int)PacketType.PacketGenericMessage, (int)GenericLobbyMessageType.LeaveGame, OnPlayerLeaveGame);            
            return result;
        }

        protected virtual GameServerGame OnCreateNewGameServerGame(Game game)
        {
            GameServerGame g = new GameServerGame(game);
            game.Decorator = g;
            return g;
        }

        protected override void OnSocketKilled(string msg)
        {            
            if (ServerUser != null && ServerUser.CurrentCharacter != null)
            {
                GameServerGame gsg = ServerUser.CurrentCharacter.GetCurrentGame();
                if (gsg != null)
                {
                    gsg.RemovePlayer(ServerUser.CurrentCharacter, "Disconnected.", true);
                    gsg.RemoveObserver(ServerUser.CurrentCharacter);
                }

                CharacterCache.UncacheCharacter(ServerUser.CurrentCharacter.ID);
            }

            base.OnSocketKilled(msg);
            DB.Instance.Server_Register(MyServer.ServerUserID, MyServer.ServerAddress, MyServer.ListenOnPort, DateTime.UtcNow, "content", ConnectionManager.PaddedConnectionCount, MyServer.MaxConnections);
        }

        /// <summary>
        /// Override HandlePacket because we dont want to incur multiple Hashtable looks for game packets
        /// </summary>
        /// <param name="p"></param>
        protected override void HandlePacket(Packet p)
        {
            if (p.PacketTypeID == (int)LobbyPacketType.GameMessage)
            {
                try
                {
                    GameServerGame g = ServerUser.CurrentCharacter.GetCurrentGame();
                    if (g == null)
                    {
                        // drop the packet
                        return;
                    }
                    g.AddGamePacketForProcessing(this, p as PacketGameMessage);
                }
                catch
                {
                    // something went wrong. drop packet.
                    return;
                }
            }
            else
            {
                base.HandlePacket(p);
            }
        }

        private static bool m_IsInitialized = false;
        protected override void OnInitialize()
        {
            base.OnInitialize();
            if (!m_IsInitialized)
            {
                RegisterPacketCreationDelegate((int)LobbyPacketType.MatchNotification, delegate { return new PacketMatchNotification(); });
                RegisterPacketCreationDelegate((int)LobbyPacketType.MatchRefresh, delegate { return new PacketMatchRefresh(); });
                RegisterPacketCreationDelegate((int)LobbyPacketType.GameMessage, delegate { return new PacketGameMessage(); });
                RegisterPacketCreationDelegate((int)LobbyPacketType.GameMessage, (int)LobbyGameMessageSubType.Chat, delegate { return new PacketGameMessage(); });
                RegisterPacketCreationDelegate((int)LobbyPacketType.GameMessage, (int)LobbyGameMessageSubType.SeatChangeRequest, delegate { return new PacketGameMessage(); });
                RegisterPacketCreationDelegate((int)LobbyPacketType.GameMessage, (int)LobbyGameMessageSubType.ClientLevelLoaded, delegate { return new PacketGameMessage(); });
                
                m_IsInitialized = true;
            }
        }              
        

    }
}
