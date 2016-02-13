using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;

namespace Shared
{
    /// <summary>
    /// Represents one Content server game/match/instance 
    /// </summary>
    public class GameServerGame : IGame
    {
        public GameServerGame(Game game)
        {
            Init(game);
        }        

        public GameServerGame()
        {
            Init(new Game(this));
        }

        private void Init(Game game)
        {
            m_Game = game;
            GameStartStrategy = new GameStartStrategy();
            GameAbortStrategy = new GameAbortStrategy();
            RegisterGamePacketHandler((int)LobbyGameMessageSubType.Chat, OnPlayerChat);
            RegisterGamePacketHandler((int)LobbyGameMessageSubType.SeatChangeRequest, OnPlayerSeatChangeRequest);
            RegisterGamePacketHandler((int)LobbyGameMessageSubType.ClientLevelLoaded, OnPlayerClientLevelLoaded);  
        }

        protected virtual void OnPlayerClientLevelLoaded(ServerUser fromPlayer, PacketGameMessage msg)
        {
            float pLoaded = msg.Parms.GetSinglelProperty("PercentLoaded").GetValueOrDefault(0);
            Log1.Logger("Server").Debug(fromPlayer.CurrentCharacter.CharacterName + " loaded " + pLoaded + "%");
            Properties.SetProperty(fromPlayer.CurrentCharacter.ID.ToString() + "_clientloadpercent", pLoaded);
            bool stillWaiting =  !m_Game.AllPlayerClientsLoaded();
            Log1.Logger("Server").Debug("Still waiting for clients to load: " + stillWaiting.ToString());
            if (!stillWaiting)
            {
                Log1.Logger("Server").Debug("Firing ALL CLIENTS LOADED");
                AllPlayerClientsLoaded();
            }
            m_Game.WaitingOnClientsToLoad = stillWaiting;
        }

        public bool WaitingOnClientsToLoad
        {
            get
            {
                return m_Game.WaitingOnClientsToLoad;
            }
        }

        /// <summary>
        /// All clients have the play level loaded and are ready to go.
        /// </summary>
        protected virtual void AllPlayerClientsLoaded()
        {
        }

        private Game m_Game;
        /// <summary>
        /// The game that this object is decorating
        /// </summary>
        public Game Game
        {
            get { return m_Game; }
            set { m_Game = value; }
        }       

        /// <summary>
        /// Current state of the game
        /// </summary>
        public GameState CurrentGameState
        {
            get
            {
                return m_Game.CurrentGameState;
            }
            set
            {
                m_Game.CurrentGameState = value;
            }
        }

        #region Messaging

        /// <summary>
        /// Gracefully transfers all players to Central
        /// </summary>
        public void TransferAllPlayersToCentral(bool includeObservers, string reason)
        {
            lock (AllPlayersSyncRoot) // don't want collection being modified as we're looping over it
            {
                List<ICharacterInfo> players = AllPlayers;
                for (int i = 0; i < players.Count; i++)
                {
                    TransferPlayerToCentral(players[i] as ServerCharacterInfo, reason);
                }
            }

            if (includeObservers)
            {
                for (int i = 0; i < Observers.Count; i++)
                {
                    ServerCharacterInfo sci = Observers[i] as ServerCharacterInfo;
                    TransferPlayerToCentral(sci, reason);
                }
            }
        }

        /// <summary>
        /// Transfers a player back to Central.  
        /// </summary>
        /// <param name="targetPlayer">the player to transfer</param>
        /// <param name="reason">a reason message that will be packaged in the GameEnded message.  If the game has already ended and that is why we are transferring the player, this paramter will be ignored.</param>
        public void TransferPlayerToCentral(ServerCharacterInfo targetPlayer, string reason)
        {
            ServerUser su = (targetPlayer == null) ? null : targetPlayer.OwningAccount;
            if (su == null)
            {
                return;
            }

            RemovePlayer(targetPlayer, reason, false);

            if (su.MyConnection != null && su.MyConnection.IsAlive)
            {                                
                // Remove player, called in line above, uses the PFX queue and we need Remove player to go off before the transfer directive - otherwise the client
                // won't get the game end message if it needs one and will thus not display the game summary.  Thus, we need to add the transfer directives to the PFX
                // queue (after the RemovePlayer call).
                //Task t = new Task((state) =>
                {
                    ((GSLobbyInboundPlayerConnection)su.MyConnection).TransferToLobbyOrDisconnect();    
                }//, "Transfer Player " + targetPlayer.ToString(), TaskCreationOptions.LongRunning);
                //m_NetQ.AddTask(t);
            }
        }

        public void SendMatchChangeNotificationToPlayers(MatchNotificationType kind, ServerCharacterInfo targetPlayer, string text, bool synchClientGameObject)
        {
            PacketMatchNotification p = new PacketMatchNotification();
            p.PacketTypeID = (int)LobbyPacketType.MatchNotification;
            p.Kind = kind;
            p.TheGame = synchClientGameObject? Game : null;
            p.TheGameID = Game.GameID;
            if (targetPlayer != null)
            {
                p.TargetPlayer = targetPlayer.CharacterInfo;
            }
            p.ReplyCode = ReplyType.OK;
            p.ReplyMessage = text;
            /*
            if (p.Kind == MatchNotificationType.MatchEnded)
            {
                // add this particular message to the PFX queue, to make sure that the EndMessage/Disconnect&Transfer happen in the property order
                Task t = new Task((state) =>
                {
                    BroadcastToPlayersInGame(p, true);
                }, "BroadcastToPlayersInGame: Match Ended", TaskCreationOptions.LongRunning);
                m_NetQ.AddTask(t);                
            }
            else */
            {
                BroadcastToPlayersInGame(p, true);
            }
        }

        public void SendMatchChangeNotificationToPlayer(ServerCharacterInfo toon, MatchNotificationType kind, ServerCharacterInfo targetPlayer, string text, bool synchClientGameObject)
        {
            PacketMatchNotification p = new PacketMatchNotification();
            p.PacketTypeID = (int)LobbyPacketType.MatchNotification;
            p.Kind = kind;
            p.TheGame = synchClientGameObject ? Game : null;
            p.TheGameID = Game.GameID;
            if (targetPlayer != null)
            {
                p.TargetPlayer = targetPlayer.CharacterInfo;
            }
            p.ReplyCode = ReplyType.OK;
            p.ReplyMessage = text;

            SendPacketToPlayer(toon, p);
        }

        public void SendGamePropertiesUpdateToPlayers(Guid bagId, Property[] ps)
        {
            PacketGamePropertiesUpdateNotification note = new PacketGamePropertiesUpdateNotification();
            note.Properties = ps;
            note.PropertyBagId = bagId;
            note.TheGame = GameID;

            BroadcastToPlayersInGame(note, true);
        }

        public void SendGamePropertieRemovedToPlayers(Guid bagId, Property[] ps)
        {
            PacketGamePropertiesUpdateNotification note = new PacketGamePropertiesUpdateNotification();
            note.Properties = ps;
            note.PropertyBagId = bagId;
            note.TheGame = GameID;
            note.Remove = true;

            BroadcastToPlayersInGame(note, true);
        }
      
        public void SendGamePropertiesUpdateToPlayer(ServerCharacterInfo toon, Guid bagId, Property[] ps)
        {
            SendGamePropertiesUpdateToPlayer(toon, bagId, ps, false, false, false);
        }

        public void SendGamePropertiesRemovedToPlayer(ServerCharacterInfo toon, Guid bagId, Property[] ps)
        {
            SendGamePropertiesUpdateToPlayer(toon, bagId, ps, true, false, false);
        }

        public void SendGamePropertiesUpdateToPlayer(ServerCharacterInfo toon, Guid bagId, Property[] ps, bool removeProperties, bool compress, bool encrypt)
        {
            PacketGamePropertiesUpdateNotification note = new PacketGamePropertiesUpdateNotification();
            note.Properties = ps;
            note.PropertyBagId = bagId;
            note.TheGame = GameID;
            note.IsCompressed = compress;
            note.IsEncrypted = encrypt;
            note.Remove = removeProperties;

            SendPacketToPlayer(toon, note);
        }

        public void SendPacketToPlayer(ServerCharacterInfo toon, Packet p)
        {
            SendPacketToPlayer(toon, p, false, false);
        }

        public void SendPacketToPlayer(ServerCharacterInfo toon, Packet p, bool compress, bool encrypt)
        {
            p.IsCompressed = compress;
            p.IsEncrypted = encrypt;

            if (toon != null && toon.OwningAccount != null && toon.OwningAccount.MyConnection != null && toon.OwningAccount.MyConnection.IsAlive)
            {
                toon.OwningAccount.MyConnection.Send(p);
            }
        }

        public void BroadcastGameMessage(int gameMessageType, PropertyBag props, bool includeObservers, bool isCompressed, bool isEncrypted)
        {
            PacketGameMessage msg = new PacketGameMessage();
            msg.IsCompressed = isCompressed;
            msg.IsEncrypted = isEncrypted;
            msg.PacketSubTypeID = gameMessageType;
            msg.Parms = props;

            BroadcastToPlayersInGame(msg, true);
        }

        public void BroadcastToPlayersInGame(Packet p, bool includeObservers)
        {
            BroadcastToPlayersInGame(p, includeObservers, null);
        }

        /// <summary>
        /// Return true if you modified the packet!
        /// </summary>
        /// <param name="p"></param>
        /// <param name="player"></param>
        /// <returns></returns>
        public virtual bool OnBeforeSendPacketToPlayer(Packet p, ServerUser player)
        {
            return false;
        }

        /// <summary>
        /// Broadcasts one packet to all the players in a game
        /// </summary>
        /// <param name="theGame">the game in question</param>
        /// <param name="p">the packet to broadcast</param>
        /// <param name="includeObservers">should the packet be sent to the observers?</param>
        public virtual void BroadcastToPlayersInGame(Packet p, bool includeObservers, ServerCharacterInfo exclude)
        {            
            byte[] rawPacket = null;
            lock (AllPlayersSyncRoot) // don't want collection being modified as we're looping over it
            {
                List<ICharacterInfo> players = AllPlayers;
                for (int i = 0; i < players.Count; i++)
                {
                    ServerCharacterInfo sci = players[i] as ServerCharacterInfo;
                    if (sci == exclude)
                    {
                        continue;
                    }

                    ServerUser su = (sci == null) ? null : sci.OwningAccount;
                    if (su == null)
                    {
                        continue;
                    }

                    if (su.MyConnection != null && su.MyConnection.IsAlive)
                    {
                        if (OnBeforeSendPacketToPlayer(p, su) || p.IsEncrypted || rawPacket == null)
                        {
                            // Optimization: no need to re-serialize the packet for different connections if it's not encrypted, so we serialize it once and then
                            // send that same data to everyone
                            rawPacket = su.MyConnection.SerializePacket(p);
                        }
                        su.MyConnection.Send(rawPacket, p.Flags);
                    }
                }
            }

            if (includeObservers)
            {

                for (int i = 0; i < Observers.Count; i++)
                {
                    ServerCharacterInfo sci = Observers[i] as ServerCharacterInfo;
                    if (sci == exclude)
                    {
                        continue;
                    }
                    ServerUser su = (sci == null) ? null : sci.OwningAccount;
                    if (su == null)
                    {
                        continue;
                    }

                    if (su.MyConnection != null && su.MyConnection.IsAlive)
                    {
                        if (OnBeforeSendPacketToPlayer(p, su) || p.IsEncrypted || rawPacket == null)
                        {
                            // Optimization: no need to re-serialize the packet for different connections if it's not encrypted, so we serialize it once and then
                            // send that same data to everyone
                            rawPacket = su.MyConnection.SerializePacket(p);
                        }
                        su.MyConnection.Send(rawPacket, p.Flags);
                    }
                }
            }

        }

        #endregion

        protected virtual void OnBeforeGameEnd()
        {
        }

        /// <summary>
        /// Notifies players that match has ended and transfers them back to central.
        /// </summary>
        protected virtual void OnGameEnded()
        {
            SendMatchChangeNotificationToPlayers(MatchNotificationType.MatchEnded, null, "", true);
            TransferAllPlayersToCentral(true, "game ended.");
        }

        protected virtual void OnBeforeGameStart()
        {
        }

        /// <summary>
        /// Gets called when the game has been started and player have been notified.
        /// </summary>
        protected virtual void OnGameStarted()
        {
            // add all currently active players ot the EverActive list
            lock (AllPlayersSyncRoot)
            {
                foreach (ICharacterInfo ci in AllPlayers)
                {
                    EverActivePlayers.Remove(ci.ID);
                    EverActivePlayers.Add(ci.ID, ci);
                }
            }
            SendMatchChangeNotificationToPlayers(MatchNotificationType.MatchStarted, null, "", false);
        }

        /// <summary>
        /// Gets called when a player is asking to start the game
        /// </summary>
        protected virtual bool OnTryStartGame(bool isPlayerRequest)
        {
            return true;
        }

        private IGameStartStrategy m_GameStartStrategy;
        /// <summary>
        /// That which determines when we can start the game
        /// </summary>
        public IGameStartStrategy GameStartStrategy
        {
            get
            {
                return m_GameStartStrategy;
            }
            set
            {
                if (m_GameStartStrategy != null)
                {
                    m_GameStartStrategy.OwningGame = null;
                }
                m_GameStartStrategy = value;
                value.OwningGame = this;
            }
        }

        private IGameAbortStrategy m_GameAbortStrategy;
        /// <summary>
        /// That which determines when a game is aborted, based on players leaving
        /// </summary>
        public IGameAbortStrategy GameAbortStrategy
        {
            get
            {
                return m_GameAbortStrategy;
            }
            set
            {
                if (m_GameAbortStrategy != null)
                {
                    m_GameAbortStrategy.OwningGame = null;
                }

                m_GameAbortStrategy = value;
                value.OwningGame = this;
            }
        }

        /// <summary>
        /// Ends the game and removes it from the server tracking
        /// </summary>
        /// <param name="reason"></param>
        public virtual void EndGame(string reason)
        {
            if (Ended) // already ended?
            {
                return;
            }
            
            // Update DB
            GameManager.Instance.RemoveGameFromServerTracking(GameID, true);            

            OnBeforeGameEnd();
            Solved = !m_Aborted;
            Ended = true;
            OnGameEnded();
            Log.LogMsg("Ending game. " + reason);            
        }

        private bool m_Aborted = false;  

        /// <summary>
        /// Aborts the game before it's conclusion and removes it from the server tracking.  Called by the
        /// GameAbortStrategy if abort conditions are met, which fires OnBeforeGameAborted and finally EndGame
        /// </summary>
        /// <param name="reason"></param>
        public virtual void AbortGame(string reason)
        {
            if (Ended) // already ended?
            {
                return;
            }
            m_Aborted = true;
            OnBeforeGameAborted();
            EndGame(reason);
            Log.LogMsg("Game being aborted... " + reason);
        }

        protected virtual void OnBeforeGameAborted()
        {
        }
               
        /// <summary>
        /// Call this method to start the game.  Method fails if the game is already in progress.
        /// If a player client is initiating the start of the game, this method fires 
        /// OnTryStartGame and, succeeding that, fires OnBeforeGameStart notifies all players
        /// that the game has started and then fires OnGameStarted.
        /// </summary>
        /// <param name="msg">failure message, if any</param>
        /// <param name="isPlayerRequest">is the start request coming from a client or is the server kicking it off?  client requests can fail if inheriting objects return false from OnTryStartGame or if the GameStartStrategy has set CanGameBeStartedManually to false</param>
        /// <returns></returns>
        public virtual bool StartGame(ref string msg, bool isPlayerRequest)
        {
            bool rslt = false;
            lock (m_Game.CurrentGameStateSyncRoot)
            {
                if (CurrentGameState == GameState.Started)
                {
                    // game already started. can't start again.
                    msg = "Game already started.";
                    return false;
                }

                if (isPlayerRequest)
                {
                    if (GameStartStrategy.CanGameBeStartedManually)
                    {
                        if (GameStartStrategy.DoesGameMeetStartConditions)
                        {
                            rslt = OnTryStartGame(isPlayerRequest);
                        }
                        else
                        {
                            msg = "Game can't be started yet.";
                        }
                    }
                    else
                    {
                        msg = "Game can't be manually started.";
                    }
                }
                else
                {
                    // system can always start the game, regardless of rules.
                    rslt = true;
                }
                
                Started = rslt;                
            }

            if (Started)
            {                
                OnBeforeGameStart();
                CurrentGameState = GameState.Started;                
                OnGameStarted();
            }
            return rslt;
        }

        /// <summary>
        /// Fires when a property in a property bag has changed
        /// </summary>
        public virtual void OnPropertyUpdated(Guid bag, Property p)
        {
            SendGamePropertiesUpdateToPlayers(bag, new Property[] { p });
        }

        /// <summary>
        /// Fires when a property in a property bag has been removed
        /// </summary>
        public virtual void OnPropertyRemoved(Guid bag, Property p)
        {
            SendGamePropertieRemovedToPlayers(bag, new Property[] { p });
        }

        /// <summary>
        /// Fires when a property in a property bag has been added
        /// </summary>
        public virtual void OnPropertyAdded(Guid bag, Property p)
        {
            SendGamePropertiesUpdateToPlayers(bag, new Property[] { p });
        }

        protected virtual void OnPlayerSeatChangeRequest(ServerUser fromPlayer, PacketGameMessage msg)
        {

        }

        protected virtual void OnPlayerChat(ServerUser fromPlayer, PacketGameMessage msg)
        {
            // Game messages are already queued... should not need to queue them manually
            //Task t = new Task((state) =>
            {
                int targetPlayer = msg.Parms.GetIntProperty("target").GetValueOrDefault(-1);
                string text = msg.Parms.GetStringProperty("text");
                if (text == null || text.Length < 1)
                {
                    return;
                }

                msg.Parms.SetProperty("sender", fromPlayer.CurrentCharacter.CharacterInfo as ISerializableWispObject);

                if (targetPlayer < 0)
                {
                    // Public chat
                    BroadcastToPlayersInGame(msg, true, fromPlayer.CurrentCharacter);
                    return;
                }

                if (IsPlayerPartOfGame(targetPlayer))
                {
                    ServerCharacterInfo sci = GetCharacter(targetPlayer) as ServerCharacterInfo;
                    SendPacketToPlayer(sci, msg);
                }

            }//, "Chat from player " + fromPlayer.CurrentCharacter.CharacterName, TaskCreationOptions.PreferFairness);
            //m_NetQ.AddTask(t);
        }
        
        /// <summary>
        /// Removes a player from the active Players list
        /// </summary>
        /// <param name="character">the character to add</param>
        public virtual void RemovePlayer(ServerCharacterInfo character, string reason, bool playerInitiated)
        {
            //Task t = new Task((state) =>
                {
                    lock (m_Game.AllPlayersSyncRoot)
                    {
                        if (Players.Remove(character.ID))
                        {                                                        
                            Log1.Logger("SERVER").Info("Removed player [#" + character.CharacterName + "] from game [" + Name + "] [" + GameID.ToString() + "].");
                            AllPlayers = (List<ICharacterInfo>)Players.Values.ToList<ICharacterInfo>();

                            if (CurrentGameState == GameState.Started && !Solved)
                            {
                                // player quit after we started the game, but before we finished the match
                                AddToQuittersRoster(character);
                            }

                            OnPlayerRemoved(character, reason, playerInitiated);                            

                            // What's the state of the game after player left?
                            if (CurrentGameState == GameState.Lobby)
                            {
                                GameStartStrategy.NotifyPlayerLeftLobby(character.ID);
                            }
                            else if (CurrentGameState == GameState.Started)
                            {
                                GameAbortStrategy.NotifyPlayerLeftGame(character.ID);
                            }
                        }
                    }
                }//, "Remove Player from game" + character.ToString(), TaskCreationOptions.LongRunning);
            //m_NetQ.AddTask(t);
        }

        /// <summary>
        /// When a player quits they are added to this games' quitters roster
        /// </summary>
        /// <param name="ci"></param>
        public void AddToQuittersRoster(ServerCharacterInfo ci)
        {
            // Add them to the games' "Quitters" roster.
            int[] playersQuit = Properties.GetIntArrayProperty("Quitters");
            if (Array.IndexOf(playersQuit, ci.ID) < 0)
            {
                Array.Resize(ref playersQuit, playersQuit.Length + 1);
                playersQuit[playersQuit.Length - 1] = ci.ID;
                Properties.SetProperty("Quitters", playersQuit);
            }
        }

        /// <summary>
        /// Checks the quitters array for the player ID
        /// </summary>
        /// <param name="toonId"></param>
        /// <returns></returns>
        public bool IsPlayerQuitter(int toonId)
        {
            return Array.IndexOf(Quitters, toonId) > -1;
        }

        /// <summary>
        /// Removes a player from the passive Observers list
        /// </summary>
        /// <param name="character">the character to add</param>
        public virtual void RemoveObserver(ServerCharacterInfo character)
        {
            //Task t = new Task((state) =>
            {
                lock (m_Game.AllObserversSyncRoot)
                {
                    if (Observers.Remove(character.ID))
                    {
                        Log.LogMsg("Removed observer [#" + character.ToString() + "] from game [" + Name + "] [" + GameID.ToString() + "].");                        
                        AllObservers = (List<ICharacterInfo>)Observers.Values.ToList<ICharacterInfo>();
                        OnObserverRemoved(character);
                    }
                }
            }//, "Remove Observer " + character.ToString(), TaskCreationOptions.LongRunning);
            //m_NetQ.AddTask(t);
        }

        /// <summary>
        /// Gets called when a player was added to the game
        /// </summary>
        /// <param name="toon">the character that was added to the game</param>
        protected virtual void OnPlayerAdded(ServerCharacterInfo toon)
        {            
            DB.Instance.Lobby_UpdateGameForServer(this);
            SendMatchChangeNotificationToPlayers(MatchNotificationType.PlayerAdded, toon, "", false);

            lock (m_Game.CurrentGameStateSyncRoot)
            {
                if (CurrentGameState == GameState.Lobby)
                {
                    GameStartStrategy.NotifyPlayerAddedToLobby(toon);
                }
                else if (CurrentGameState == GameState.Started)
                {
                    GameAbortStrategy.NotifyPlayerAddedToGame(toon);
                }
            }
        }

        /// <summary>
        /// Gets called when an observer is added to the game
        /// </summary>
        /// <param name="character">the character that was added</param>
        protected virtual void OnObserverAdded(ServerCharacterInfo character)
        {            
            SendMatchChangeNotificationToPlayers(MatchNotificationType.ObserverAdded, character, "", false);
            DB.Instance.Lobby_UpdateGameForServer(this);
        }        

        /// <summary>
        /// Gets called when a player is removed from the game
        /// </summary>
        /// <param name="character"></param>
        protected virtual void OnPlayerRemoved(ServerCharacterInfo toon, string reason, bool playerInitiated)
        {
            toon.SetCurrentGame(null);

            // Update game stats in game listing DB
            DB.Instance.Lobby_UpdateGameForServer(this);

            // Send notification to player that player left, including game state update. Player is no longer part of "AllPlayers"  list at this point, so we
            // have to manually send this notification to them.
            SendGamePropertiesUpdateToPlayer(toon, this.Properties.ID, this.Properties.AllProperties);
            SendMatchChangeNotificationToPlayer(toon, MatchNotificationType.PlayerRemoved, toon, reason, false);
            
            // Broadcast to all remaining players
            SendMatchChangeNotificationToPlayers(MatchNotificationType.PlayerRemoved, toon, reason, false);
            SendGamePropertiesUpdateToPlayers(this.Properties.ID, this.Properties.AllProperties);

            // Set new game owner if necessary
            if (Owner == toon.ID)
            {
                if (AllPlayers.Count > 0)
                {
                    Owner = AllPlayers[0].ID;
                    BroadcastGameInfoMessage(AllPlayers[0].CharacterName + " is now the owner of this game.", true);
                    PropertyBag props = new PropertyBag();
                    props.SetProperty("NewOwner", Owner);
                    BroadcastGameMessage((int)LobbyGameMessageSubType.NewOwner, props, true, false, false);
                }
            }

            // when a player leaves the game, they go back to Central
            if (CurrentGameState == GameState.Lobby || CurrentGameState == GameState.Started)
            {
                string address = "";
                int port = 0;
                string serverId = "";
                if (!GSLobbyInboundPlayerConnection.GetCentralHandoffAddress(ref address, ref port, ref serverId))
                {
                    toon.OwningAccount.MyConnection.KillConnection("Unable to host player on this server.  No lobby server found to hand off too after leaving game.");
                    return;
                }
                toon.OwningAccount.TransferToServerUnassisted(address, port, Guid.Empty, "Lobby Server", serverId);
            }
        }

        /// <summary>
        /// Gets called when a player is removed from the observers
        /// </summary>
        /// <param name="character">the player that was removed</param>
        protected virtual void OnObserverRemoved(ServerCharacterInfo character)
        {            
            character.SetCurrentGame(null);
            SendMatchChangeNotificationToPlayers(MatchNotificationType.ObserverRemoved, character, "", false);
            DB.Instance.Lobby_UpdateGameForServer(this);
        }
       
        /// <summary>
        /// Gets called when a player is about to be added.  Return false to prevent player from being added.
        /// </summary>
        /// <param name="toon">the character to add</param>
        /// <returns></returns>
        protected virtual bool CanAddPlayer(ServerCharacterInfo toon, ref string msg)
        {
            msg = "";
            if (Players.ContainsKey(toon.ID))
            {
                msg = "Player is already part of this game.";
                return false;
            }

            if (PlayerCountSafe + 1 > MaxPlayers)
            {
                msg = "Game is full.  Sorry.";
                return false;
            }
            return true;
        }

        /// <summary>
        /// Adds a player to the active Players list
        /// </summary>
        /// <param name="character">the character to add</param>
        /// <returns></returns>
        public virtual bool AddPlayer(ServerCharacterInfo character, ref string msg)
        {
            lock (m_Game.AllPlayersSyncRoot)
            {
                if (Players.ContainsKey(character.ID) || !CanAddPlayer(character, ref msg))
                {
                    return false;
                }

                if (CurrentGameState == GameState.Started)
                {
                    // player was adde after game started.  add to active list
                    EverActivePlayers.Remove(character.ID);
                    EverActivePlayers.Add(character.ID, character);
                }

                Players.Add(character.ID, character);
                character.SetCurrentGame(this);
                AllPlayers = (List<ICharacterInfo>)Players.Values.ToList<ICharacterInfo>();
                OnPlayerAdded(character);                
            }

            return true;
        }

        /// <summary>
        /// Gets a reference to the character, given the character ID. If that character is not part of this game, null is returned.
        /// </summary>
        /// <param name="id">id of the character to return</param>
        /// <returns></returns>
        public ICharacterInfo GetCharacter(int id)
        {
            ICharacterInfo toon = null;
            lock (m_Game.AllPlayersSyncRoot)
            {
                Players.TryGetValue(id, out toon);
            }

            return toon;
        }

        /// <summary>
        /// Gets called when an observers is about to be added.  Return false to prevent player from being added.
        /// </summary>
        /// <param name="character">the player to observe</param>
        /// <returns></returns>
        protected virtual bool CanAddObserver(ServerCharacterInfo character, ref string msg)
        {
            msg = "";
            if (Observers.ContainsKey(character.ID))
            {
                msg = character.CharacterName + " is already observing the match.";
                return false;
            }

            if (ObserverCountSafe + 1 > MaxObservers)
            {
                msg = "Game has no more seats available for observers.  Sorry.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Adds a player to the passive Observers list
        /// </summary>
        /// <param name="character">the character to add</param>
        /// <returns></returns>
        public virtual bool AddObserver(ServerCharacterInfo character, ref string msg)
        {
            lock (m_Game.AllObserversSyncRoot)
            {
                if (!CanAddObserver(character, ref msg))
                {
                    return false;
                }

                Observers.Add(character.ID, character);
                character.SetCurrentGame(this);
                AllObservers = (List<ICharacterInfo>)Observers.Values.ToList<ICharacterInfo>();
                OnObserverAdded(character);
            }

            return true;
        }

        /// <summary>
        /// The current number of player in the game. Use this to get a threadsafe count of players.
        /// </summary>
        public int PlayerCountSafe
        {
            get
            {
                lock (AllPlayersSyncRoot)
                {
                    return AllPlayers.Count;
                }
            }
        }

        /// <summary>
        /// The current number of observers in the game. Use this to get a threadsafe count of observers.
        /// </summary>
        public int ObserverCountSafe
        {
            get
            {
                lock (AllObserversSyncRoot)
                {
                    return AllObservers.Count;
                }
            }
        }

        public List<ICharacterInfo> AllPlayers
        {
            get
            {
                return m_Game.AllPlayers;
            }
            set
            {
                m_Game.AllPlayers = value;
            }
        }

        public List<ICharacterInfo> AllObservers
        {
            get
            {
                return m_Game.AllObservers;
            }
            set
            {
                m_Game.AllObservers = value;
            }
        }

        public Dictionary<int, ICharacterInfo> Observers
        {
            get
            {
                return m_Game.Observers;
            }
            set
            {
                m_Game.Observers = value;
            }
        }

        public bool Ended
        {
            get
            {
                return m_Game.Ended;
            }
            set
            {
                m_Game.Ended = value;
            }
        }

        public Guid GameID
        {
            get
            {
                return m_Game.GameID;
            }
            set
            {
                m_Game.GameID = value;
            }
        }

        public bool IsShuttingDown
        {
            get
            {
                return m_Game.IsShuttingDown;
            }
            set
            {
                m_Game.IsShuttingDown = value;
            }
        }

        public int MaxPlayers
        {
            get { return m_Game.MaxPlayers; }
            set 
            { 
                m_Game.MaxPlayers = value;
                DB.Instance.Lobby_UpdateGameForServer(this);
            }
        }

        public int MaxObservers
        {
            get { return m_Game.MaxObservers; }
            set { m_Game.MaxObservers = value; }
        }

        public string Name
        {
            get { return m_Game.ComponentName; }
            set 
            { 
                m_Game.ComponentName = value;
                DB.Instance.Lobby_UpdateGameForServer(this);
            }
        }        

        public int Owner
        {
            get
            {
                return m_Game.Owner;
            }
            set
            {
                m_Game.Owner = value;
            }
        }

        public Dictionary<int, ICharacterInfo> Players
        {
            get
            {
                return m_Game.Players;
            }
            set
            {
                m_Game.Players = value;
            }
        }

        public Dictionary<int, ICharacterInfo> EverActivePlayers
        {
            get
            {
                return m_Game.EverActivePlayers;
            }
            set
            {
                m_Game.EverActivePlayers = value;
            }
        }

        public PropertyBag Properties
        {
            get
            {
                return m_Game.Properties;
            }
            set
            {
                m_Game.Properties = value;
            }
        }

        public bool Solved
        {
            get
            {
                return m_Game.Solved;
            }
            set
            {
                m_Game.Solved = value;
            }
        }

        public bool Started
        {
            get
            {
                return m_Game.Started;
            }
            set
            {
                m_Game.Started = value;
            }
        }

        public object AllPlayersSyncRoot
        {
            get { return m_Game.AllPlayersSyncRoot; }
        }

        /// <summary>
        /// Checks to see if the player is part of this game, currently
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool IsPlayerPartOfGame(int id)
        {
            return Game.IsPlayerPartOfGame(id);
        }

        /// <summary>
        /// Checks to see if that player currently an observer
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool IsPlayerObserver(int id)
        {
            return Game.IsPlayerObserver(id);
        }

        //////////////////////////////////////////////////

        void IComponent.AddComponent(IComponent c)
        {
            throw new NotImplementedException();
        }

        void IComponent.ClearComponents()
        {
            throw new NotImplementedException();
        }

        void IComponent.Deserialize(byte[] data, Pointer p, bool includeSubComponents)
        {
            throw new NotImplementedException();
        }

        List<IComponent> IComponent.GetAllComponents()
        {
            throw new NotImplementedException();
        }

        T IComponent.GetComponent<T>()
        {
            throw new NotImplementedException();
        }

        IEnumerator<IComponent> IComponent.GetComponentEnumerator()
        {
            throw new NotImplementedException();
        }

        List<T> IComponent.GetComponentsOfType<T>()
        {
            throw new NotImplementedException();
        }

        string IComponent.ComponentName
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        void IComponent.RemoveComponent(IComponent c)
        {
            throw new NotImplementedException();
        }

        void IComponent.Serialize(ref byte[] buffer, Pointer p, bool includeSubComponents)
        {
            throw new NotImplementedException();
        }

        uint IComponent.TypeHash
        {
            get { throw new NotImplementedException(); }
        }

        public string ComponentName
        {
            get
            {
                return m_Game.ComponentName;
            }
            set
            {
                m_Game.ComponentName = value;
            }
        }

        public object AllObserversSyncRoot
        {
            get
            {
                return m_Game.AllObserversSyncRoot;
            }
        }

        /// <summary>
        /// Sends a textual informational message to all players
        /// </summary>
        /// <param name="msg">the message</param>
        /// <param name="includeObservers">if observers should be included</param>
        /// <param name="targetPlayers">explicit list of message targets, or null for everyone</param>
        public void BroadcastGameInfoMessage(string msg, bool includeObservers)
        {
            PacketGameInfoNotification note = new PacketGameInfoNotification();
            note.Message = msg;
            BroadcastToPlayersInGame(note, includeObservers);
        }


        /// <summary>
        /// Sends a textual informational message to a player
        /// </summary>
        /// <param name="msg">the message</param>
        /// <param name="includeObservers">if observers should be included</param>
        /// <param name="targetPlayers">explicit list of message targets, or null for everyone</param>
        public void SendGameInfoMessageToPlayer(ServerCharacterInfo toon, string msg)
        {
            PacketGameInfoNotification note = new PacketGameInfoNotification();
            note.Message = msg;
            SendPacketToPlayer(toon, note);
        }

        /// <summary>
        /// Sends a generic PacketGameMessage of a specific sub type to a player
        /// </summary>
        /// <param name="msg">the message</param>
        /// <param name="includeObservers">if observers should be included</param>
        /// <param name="targetPlayers">explicit list of message targets, or null for everyone</param>
        public void SendGameMessageToPlayer(ServerCharacterInfo toon, int subType, PropertyBag props)
        {
            PacketGameMessage note = new PacketGameMessage();
            note.PacketSubTypeID = subType;
            note.Parms = props;
            
            SendPacketToPlayer(toon, note);
        }

        private GenericHandlerMap<ServerUser, PacketGameMessage> m_PacketHandlers = new GenericHandlerMap<ServerUser, PacketGameMessage>();
        private PFXSingleTaskQueue m_NetQ = new PFXSingleTaskQueue();
        
        /// <summary>
        /// Gets called when a game packet is about to be processed.  Return false to not process it.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        protected virtual bool OnBeforeHandleGamePacket(ServerUser user, PacketGameMessage msg)
        {
            return true;
        }

        /// <summary>
        /// Adds a packet to the queue for serial processing.
        /// </summary>
        /// <param name="con">the connection which sent the packet</param>
        /// <param name="msg">the packet</param>
        public void AddGamePacketForProcessing(GSLobbyInboundPlayerConnection con, PacketGameMessage msg)
        {
            Action<object> lambda = (state) =>
            {
                Action<ServerUser, PacketGameMessage> handler = m_PacketHandlers.GetHandlerDelegate(msg.PacketSubTypeID);
                if (handler != null && OnBeforeHandleGamePacket(con.ServerUser, msg))
                {
                    try
                    {
                        handler(con.ServerUser, msg);
                    }
                    catch (Exception e)
                    {
                        Log.LogMsg("Exception thrown whilst processing game packet type " + msg.PacketTypeID.ToString() + ", sub-type " + msg.PacketSubTypeID + ". Object = " + this.GetType().ToString() + ", Message: " + e.Message + ". Stack:\r\n " + e.StackTrace);                        
                    }

                    con.OnAfterPacketProcessed(msg);
                    return;
                }
                con.KillConnection("Did not have a registered game packet handler for game packet. " + msg.PacketTypeID.ToString() + ", SubType " + msg.PacketSubTypeID.ToString() + ". ");
            };

            // if we're not processing packets immediately, then this method (AddGamePacketForProcessing) is being called as part of  a tight networking processing loop
            // which is executed serially, so we need to NOT queue the packet in that case and just run it
            if (!con.ProcessIncomingPacketsImmediately) 
            {
                lambda(null);
            }
            else
            {
                Task t = new Task(lambda, "Game [" + GameID.ToString() + "] Process game packet " + msg.PacketSubTypeID.ToString(), TaskCreationOptions.LongRunning);
                m_NetQ.AddTask(t);
            }
        }

        protected void RegisterGamePacketHandler(int gamePacketSubType, Action<ServerUser, PacketGameMessage> handler)
        {
            m_PacketHandlers.RegisterHandler(gamePacketSubType, handler);
        }

        protected void UnregisterGamePacketHandler(int gamePacketSubType, Action<ServerUser, PacketGameMessage> handler)
        {
            m_PacketHandlers.UnregisterHandler(gamePacketSubType, handler);
        }

        /// <summary>
        /// Players who have quit the game since it started.
        /// </summary>
        public int[] Quitters
        {
            get { return m_Game.Quitters; }
        }
    }
}
