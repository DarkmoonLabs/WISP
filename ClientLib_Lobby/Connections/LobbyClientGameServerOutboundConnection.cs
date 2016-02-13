using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace Shared
{
    public delegate void CreateGameRequestResolvedDelegate(bool success, string msg, ClientServerOutboundConnection sender, Game newGame);
    public delegate void PlayerJoinedDelegate(bool isFromCentralServer, bool isFromContentServer, bool success, string msg, ClientServerOutboundConnection sender, CharacterInfo player, Game theGame, PropertyBag parms);
    public delegate void PlayerRemovedDelegate(string msg, ClientServerOutboundConnection sender, CharacterInfo player, Game theGame, PropertyBag parms);
    public delegate void GameStartReplyDelegate(LobbyClientGameServerOutboundConnection con, bool result, string msg);
    public delegate void GameEndDelegate(LobbyClientGameServerOutboundConnection con, PropertyBag finalProperties, string msg);

    /// <summary>
    ///  Represent one connection to a lobby type game server
    /// </summary>
    public class LobbyClientGameServerOutboundConnection : ClientGameServerOutboundConnection
    {
        #region ContentCreateGameRequestResolved Event
        private CreateGameRequestResolvedDelegate ContentCreateGameRequestResolvedInvoker;

        /// <summary>
        /// Fires after we connect to the content server and try to instantiate our game instance
        /// </summary>
        public event CreateGameRequestResolvedDelegate ContentCreateGameRequestResolved
        {
            add
            {
                AddHandler_ContentCreateGameRequestResolved(value);
            }
            remove
            {
                RemoveHandler_ContentCreateGameRequestResolved(value);
            }
        }

        
        private void AddHandler_ContentCreateGameRequestResolved(CreateGameRequestResolvedDelegate value)
        {
            ContentCreateGameRequestResolvedInvoker = (CreateGameRequestResolvedDelegate)Delegate.Combine(ContentCreateGameRequestResolvedInvoker, value);
        }

        
        private void RemoveHandler_ContentCreateGameRequestResolved(CreateGameRequestResolvedDelegate value)
        {
            ContentCreateGameRequestResolvedInvoker = (CreateGameRequestResolvedDelegate)Delegate.Remove(ContentCreateGameRequestResolvedInvoker, value);
        }

        private void FireContentCreateGameRequestResolved(bool result, string msg, ClientServerOutboundConnection con, Game g)
        {
            if (ContentCreateGameRequestResolvedInvoker != null)
            {
                ContentCreateGameRequestResolvedInvoker(result, msg, con, g);
            }
        }
        #endregion

        #region JoinGameResolved Event
        private JoinGameResultDelegate JoinGameResolvedInvoker;


        /// <summary>
        /// Fires after we connect to the content server and try to join our target game.  If the join request is successful, the PlayerJoined
        /// will also fire after this event resolves.
        /// </summary>
        public event JoinGameResultDelegate JoinGameResolved
        {
            add
            {
                AddHandler_JoinGameResolved(value);
            }
            remove
            {
                RemoveHandler_JoinGameResolved(value);
            }
        }

        
        private void AddHandler_JoinGameResolved(JoinGameResultDelegate value)
        {
            JoinGameResolvedInvoker = (JoinGameResultDelegate)Delegate.Combine(JoinGameResolvedInvoker, value);
        }

        
        private void RemoveHandler_JoinGameResolved(JoinGameResultDelegate value)
        {
            JoinGameResolvedInvoker = (JoinGameResultDelegate)Delegate.Remove(JoinGameResolvedInvoker, value);
        }

        private void FireJoinGameResolved(bool result, string msg, ClientServerOutboundConnection con, Game g)
        {
            if (JoinGameResolvedInvoker != null)
            {
                JoinGameResolvedInvoker(result, msg, con, g);
            }
        }
        #endregion

        #region PlayerJoined Event
        private PlayerJoinedDelegate PlayerJoinedInvoker;

        /// <summary>
        /// Fires when a player joins a game.  This will also fire when we join a game.
        /// </summary>
        public event PlayerJoinedDelegate PlayerJoined
        {
            add
            {
                AddHandler_PlayerJoined(value);
            }
            remove
            {
                RemoveHandler_PlayerJoined(value);
            }
        }

        
        private void AddHandler_PlayerJoined(PlayerJoinedDelegate value)
        {
            PlayerJoinedInvoker = (PlayerJoinedDelegate)Delegate.Combine(PlayerJoinedInvoker, value);
        }

        
        private void RemoveHandler_PlayerJoined(PlayerJoinedDelegate value)
        {
            PlayerJoinedInvoker = (PlayerJoinedDelegate)Delegate.Remove(PlayerJoinedInvoker, value);
        }

        private void FirePlayerJoined(bool isFromCentralServer, bool isFromContentServer, bool result, string msg, ClientServerOutboundConnection con, CharacterInfo player, Game g, PropertyBag parms)
        {
            if (PlayerJoinedInvoker != null)
            {
                PlayerJoinedInvoker(isFromCentralServer, isFromContentServer, result, msg, con, player, g, parms);
            }
        }
        #endregion

        #region ObserverJoined Event
        private PlayerJoinedDelegate ObserverJoinedInvoker;

        /// <summary>
        /// Fires when an Observer joins a game.  This will also fire when we join a game.
        /// </summary>
        public event PlayerJoinedDelegate ObserverJoined
        {
            add
            {
                AddHandler_ObserverJoined(value);
            }
            remove
            {
                RemoveHandler_ObserverJoined(value);
            }
        }

        
        private void AddHandler_ObserverJoined(PlayerJoinedDelegate value)
        {
            ObserverJoinedInvoker = (PlayerJoinedDelegate)Delegate.Combine(ObserverJoinedInvoker, value);
        }

        
        private void RemoveHandler_ObserverJoined(PlayerJoinedDelegate value)
        {
            ObserverJoinedInvoker = (PlayerJoinedDelegate)Delegate.Remove(ObserverJoinedInvoker, value);
        }

        private void FireObserverJoined(bool isFromCentralServer, bool isFromContentServer, bool result, string msg, ClientServerOutboundConnection con, CharacterInfo Observer, Game g, PropertyBag parms)
        {
            if (ObserverJoinedInvoker != null)
            {
                ObserverJoinedInvoker(isFromCentralServer, isFromContentServer, result, msg, con, Observer, g, parms);
            }
        }
        #endregion

        #region PlayerRemoved Event
        private PlayerRemovedDelegate PlayerRemovedInvoker;

        /// <summary>
        /// Fires when a player is removed (including us) from a game
        /// </summary>
        public event PlayerRemovedDelegate PlayerRemoved
        {
            add
            {
                AddHandler_PlayerRemoved(value);
            }
            remove
            {
                RemoveHandler_PlayerRemoved(value);
            }
        }

        
        private void AddHandler_PlayerRemoved(PlayerRemovedDelegate value)
        {
            PlayerRemovedInvoker = (PlayerRemovedDelegate)Delegate.Combine(PlayerRemovedInvoker, value);
        }

        
        private void RemoveHandler_PlayerRemoved(PlayerRemovedDelegate value)
        {
            PlayerRemovedInvoker = (PlayerRemovedDelegate)Delegate.Remove(PlayerRemovedInvoker, value);
        }

        private void FirePlayerRemoved(string msg, ClientServerOutboundConnection con, CharacterInfo player, Game g, PropertyBag parms)
        {
            if (PlayerRemovedInvoker != null)
            {
                PlayerRemovedInvoker(msg, con, player, g, parms);
            }
        }
        #endregion

        #region ObserverRemoved Event
        private PlayerRemovedDelegate ObserverRemovedInvoker;

        /// <summary>
        /// Fires when a player is removed (including us) as an observer from a game.
        /// </summary>
        public event PlayerRemovedDelegate ObserverRemoved
        {
            add
            {
                AddHandler_ObserverRemoved(value);
            }
            remove
            {
                RemoveHandler_ObserverRemoved(value);
            }
        }

        
        private void AddHandler_ObserverRemoved(PlayerRemovedDelegate value)
        {
            ObserverRemovedInvoker = (PlayerRemovedDelegate)Delegate.Combine(ObserverRemovedInvoker, value);
        }

        
        private void RemoveHandler_ObserverRemoved(PlayerRemovedDelegate value)
        {
            ObserverRemovedInvoker = (PlayerRemovedDelegate)Delegate.Remove(ObserverRemovedInvoker, value);
        }

        private void FireObserverRemoved(string msg, ClientServerOutboundConnection con, CharacterInfo player, Game g, PropertyBag parms)
        {
            if (ObserverRemovedInvoker != null)
            {
                ObserverRemovedInvoker(msg, con, player, g, parms);
            }
        }
        #endregion

        #region GameStartReply Event
        private GameStartReplyDelegate GameStartReplyInvoker;

        /// <summary>
        /// Fires in response to our request to start the current game.
        /// </summary>
        public event GameStartReplyDelegate GameStartReply
        {
            add
            {
                AddHandler_GameStartReply(value);
            }
            remove
            {
                RemoveHandler_GameStartReply(value);
            }
        }

        
        private void AddHandler_GameStartReply(GameStartReplyDelegate value)
        {
            GameStartReplyInvoker = (GameStartReplyDelegate)Delegate.Combine(GameStartReplyInvoker, value);
        }

        
        private void RemoveHandler_GameStartReply(GameStartReplyDelegate value)
        {
            GameStartReplyInvoker = (GameStartReplyDelegate)Delegate.Remove(GameStartReplyInvoker, value);
        }

        private void FireGameStartReply(LobbyClientGameServerOutboundConnection con, bool result, string msg)
        {
            if (GameStartReplyInvoker != null)
            {
                GameStartReplyInvoker(con, result, msg);
            }
        }
        #endregion

        #region GameEnded Event
        private GameEndDelegate GameEndedInvoker;

        /// <summary>
        /// Fires when a game concludes.
        /// </summary>
        public event GameEndDelegate GameEnded
        {
            add
            {
                AddHandler_GameEnded(value);
            }
            remove
            {
                RemoveHandler_GameEnded(value);
            }
        }

        
        private void AddHandler_GameEnded(GameEndDelegate value)
        {
            GameEndedInvoker = (GameEndDelegate)Delegate.Combine(GameEndedInvoker, value);
        }

        
        private void RemoveHandler_GameEnded(GameEndDelegate value)
        {
            GameEndedInvoker = (GameEndDelegate)Delegate.Remove(GameEndedInvoker, value);
        }

        private void FireGameEnded(LobbyClientGameServerOutboundConnection con, PropertyBag gameProps, string msg)
        {
            if (GameEndedInvoker != null)
            {
                GameEndedInvoker(con, gameProps, msg);
            }
        }
        #endregion

        #region GameActivating Event
        public delegate ClientGame GameActivatingDelegate(LobbyClientGameServerOutboundConnection con, Game gameData);

        private GameActivatingDelegate GameActivatingInvoker;

        /// <summary>
        /// Fires when a game concludes.
        /// </summary>
        public event GameActivatingDelegate GameActivating
        {
            add
            {
                AddHandler_GameActivating(value);
            }
            remove
            {
                RemoveHandler_GameActivating(value);
            }
        }


        private void AddHandler_GameActivating(GameActivatingDelegate value)
        {
            GameActivatingInvoker = (GameActivatingDelegate)Delegate.Combine(GameActivatingInvoker, value);
        }


        private void RemoveHandler_GameActivating(GameActivatingDelegate value)
        {
            GameActivatingInvoker = (GameActivatingDelegate)Delegate.Remove(GameActivatingInvoker, value);
        }

        private ClientGame FireGameActivating(LobbyClientGameServerOutboundConnection con, Game gameData)
        {
            if (GameActivatingInvoker != null)
            {
                return GameActivatingInvoker(con, gameData);
            }
            return null;
        }
        #endregion

        private ClientGame m_CurrentGame;

        /// <summary> 
        /// The game that we are currently playing in
        /// </summary>
        public ClientGame CurrentGame
        {
            get { return m_CurrentGame; }
            set { m_CurrentGame = value; }
        }
        
        public LobbyClientGameServerOutboundConnection(bool isBlocking)
            : base(isBlocking)
        {
        }

        protected override void OnServerLoginResponse(PacketLoginResult packetLoginResult)
        {
            if (packetLoginResult.ReplyCode == ReplyType.OK)
            {
                Game g = packetLoginResult.Parms.GetWispProperty("TargetGame") as Game;
                if (g != null)
                {
                    CurrentGame = FireGameActivating(this, g);
                }
            }
            base.OnServerLoginResponse(packetLoginResult);
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            RegisterPacketHandler((int)LobbyPacketType.MatchNotification,  OnGameNotification);
            NetworkConnection.RegisterPacketCreationDelegate((int)LobbyPacketType.GameMessage, delegate { return new PacketGameMessage(); });
            NetworkConnection.RegisterPacketCreationDelegate((int)LobbyPacketType.GameMessage, (int)LobbyGameMessageSubType.GameInfoNotification, delegate { return new PacketGameInfoNotification(); });
            NetworkConnection.RegisterPacketCreationDelegate((int)LobbyPacketType.GameMessage, (int)LobbyGameMessageSubType.GamePropertiesUpdateNotification, delegate { return new PacketGamePropertiesUpdateNotification(); });
            NetworkConnection.RegisterPacketCreationDelegate((int)LobbyPacketType.GameMessage, (int)LobbyGameMessageSubType.Chat, delegate { return new PacketGameMessage(); });
            NetworkConnection.RegisterPacketCreationDelegate((int)LobbyPacketType.GameMessage, (int)LobbyGameMessageSubType.NewOwner, delegate { return new PacketGameMessage(); });
            RegisterStandardPacketReplyHandler((int)PacketType.PacketGenericMessage, (int)GenericLobbyMessageType.RequestStartGame, OnGameStartReply);
        }
        
        protected virtual void OnGameCreated(PacketMatchNotification note)
        {
            if (note.TheGame != null)
            {
                Log.LogMsg("Game [" + note.TheGame.GameID.ToString() + "] created: " + note.ReplyCode.ToString() + " - " + note.ReplyMessage);
            }
            else
            {
                Log.LogMsg("Game created: " + note.ReplyCode.ToString() + " - " + note.ReplyMessage);
            }

            FireContentCreateGameRequestResolved(note.ReplyCode == ReplyType.OK, note.ReplyMessage, this, note.TheGame as Game);
        }

        protected void OnGameStartReply(INetworkConnection con, Packet p)
        {            
            PacketReply rep = p as PacketReply;
            if (rep.ReplyCode != ReplyType.OK)
            {
                // only fire this event if it the request fails.
                // if the start request succeeds, then OnGameStarted will fire
                FireGameStartReply(this, false, rep.ReplyMessage);
            }
        }

        private void OnGameStarted(PacketMatchNotification note)
        {
            if (CurrentGame != null)
            {
                foreach (ICharacterInfo ci in CurrentGame.AllPlayers)
                {
                    CurrentGame.EverActivePlayers.Remove(ci.ID);
                    CurrentGame.EverActivePlayers.Add(ci.ID, ci);
                }
            }
            FireGameStartReply(this, true, note.ReplyMessage);
        }

        private void OnGameEnded(PacketMatchNotification note)
        {
            FireGameEnded(this, note.TheGame.Properties, note.ReplyMessage);
            this.CurrentGame = null;
        }

        protected void OnGameNotification(INetworkConnection con, Packet p)
        {
            PacketMatchNotification note = p as PacketMatchNotification;

            if (CurrentGame != null)
            {
                if (CurrentGame.GameID != note.TheGameID)
                {
                    return;
                }               
            }

            switch (note.Kind)
            {
                case MatchNotificationType.PlayerRemoved:
                    OnPlayerRemovedFromGame(note);
                    break;
                case MatchNotificationType.PlayerAdded:
                    OnPlayerAddedToGame(note);
                    break;
                case MatchNotificationType.MatchCreated:
                    OnGameCreated(note);
                    break;
                case MatchNotificationType.ObserverAdded:
                    OnObserverAdded(note);
                    break;
                case MatchNotificationType.ObserverRemoved:
                    OnObserverRemoved(note);
                    break;
                case MatchNotificationType.MatchStarted:
                    OnGameStarted(note);
                    break;
                case MatchNotificationType.MatchEnded :
                    OnGameEnded(note);
                    break;                    
                default:
                    Log.LogMsg("Unhandled packet match notification [" + note.Kind.ToString() + "]");
                    break;
            }
        }

        private void OnPlayerAddedToGame(PacketMatchNotification note)
        {
            if (CurrentGame == null)
            {
                return;
            }

            // Update player objects
            lock (CurrentGame.AllPlayersSyncRoot)
            {
                if (note.TheGame != null) // sync all player objects
                {
                    CurrentGame.Players = note.TheGame.Players;
                    if (CurrentGame.CurrentGameState == GameState.Started)
                    {
                        foreach (ICharacterInfo ci in note.TheGame.Players.Values)
                        {
                            CurrentGame.EverActivePlayers.Remove(ci.ID);
                            CurrentGame.EverActivePlayers.Add(ci.ID, ci);
                        }
                    }
                }

                if (note.TargetPlayer != null) // update target player object
                {
                    // update the toon with the incoming data                    
                    CurrentGame.Players.Remove(note.TargetPlayer.ID);
                    CurrentGame.Players.Add(note.TargetPlayer.ID, note.TargetPlayer);
                    
                    if (CurrentGame.CurrentGameState == GameState.Started)
                    {
                        CurrentGame.EverActivePlayers.Remove(note.TargetPlayer.ID);
                        CurrentGame.EverActivePlayers.Add(note.TargetPlayer.ID, note.TargetPlayer);
                    }
                }                

                CurrentGame.AllPlayers = (List<ICharacterInfo>)CurrentGame.Players.Values.ToList<ICharacterInfo>();
            }

            if (Client.Instance.User.CurrentCharacter.ID == note.TargetPlayer.ID) // was this message about me?
            {
                // Join message about us only come as a result of our join requests.  Therefore, we must fire the following event...
                FireJoinGameResolved(note.ReplyCode == ReplyType.OK, note.ReplyMessage, this, note.TheGame as Game);
            }

            Log.LogMsg("Player [" + note.TargetPlayer != null ? note.TargetPlayer.CharacterName : "Some Player" + "] JOINED game [" + note.TheGameID + "]");
            FirePlayerJoined(false, true, note.ReplyCode == ReplyType.OK, note.ReplyMessage, this, note.TargetPlayer as CharacterInfo, note.TheGame as Game, note.Parms);
        }

        private void OnObserverAdded(PacketMatchNotification note)
        {
            if (CurrentGame == null)
            {
                return;
            }

            // Update Observer objects
            lock (CurrentGame.AllObserversSyncRoot)
            {
                if (note.TheGame != null) // sync all observer objects
                {
                    CurrentGame.Observers = note.TheGame.Observers;
                }

                if (note.TargetPlayer != null) // update target player object
                {
                    // update the toon with the incoming data                    
                    CurrentGame.Observers.Remove(note.TargetPlayer.ID);
                    CurrentGame.Observers.Add(note.TargetPlayer.ID, note.TargetPlayer);
                }
                CurrentGame.AllObservers = (List<ICharacterInfo>)CurrentGame.Observers.Values.ToList<ICharacterInfo>();
            }

            Log.LogMsg("Player [" + note.TargetPlayer != null ? note.TargetPlayer.CharacterName : "Some Player" + "] IS OBSERVING game [" + note.TheGameID + "]");
            FireObserverJoined(false, true, note.ReplyCode == ReplyType.OK, note.ReplyMessage, this, note.TargetPlayer as CharacterInfo, note.TheGame as Game, note.Parms);
        }

        private void OnPlayerRemovedFromGame(PacketMatchNotification note)
        {
            if (CurrentGame == null)
            {
                return;
            }

            // Update player objects
            lock (CurrentGame.AllPlayersSyncRoot)
            {
                if (note.TheGame != null) // sync all player objects
                {
                    CurrentGame.Players = note.TheGame.Players;
                }

                if (note.TargetPlayer != null) // update target player object
                {
                    // update the toon with the incoming data                    
                    CurrentGame.Players.Remove(note.TargetPlayer.ID);
                }

                CurrentGame.AllPlayers = (List<ICharacterInfo>)CurrentGame.Players.Values.ToList<ICharacterInfo>();
            }

            Log.LogMsg("Player [" + note.TargetPlayer != null ? note.TargetPlayer.CharacterName : "Some Player" + "] STOPPED PLAYING game [" + note.TheGameID + "]");
            FirePlayerRemoved( note.ReplyMessage, this, note.TargetPlayer as CharacterInfo, note.TheGame as Game, note.Parms);                           

            if (Client.Instance.User != null && Client.Instance.User.CurrentCharacter != null)
            {
                if (Client.Instance.User.CurrentCharacter.ID == note.TargetPlayer.ID) // did I get removed?
                {
                    // yes, i was removed from the game. we are no longer part of a game.
                    CurrentGame = null;
                }
            }
        }

        private void OnObserverRemoved(PacketMatchNotification note)
        {
            if (CurrentGame == null)
            {
                return;
            }

            // Update Observer objects
            lock (CurrentGame.AllObserversSyncRoot)
            {
                if (note.TheGame != null) // sync all observer objects
                {
                    CurrentGame.Observers = note.TheGame.Observers;
                }

                if (note.TargetPlayer != null) // update target player object
                {
                    // update the toon with the incoming data                    
                    CurrentGame.Observers.Remove(note.TargetPlayer.ID);
                }
                CurrentGame.AllObservers = (List<ICharacterInfo>)CurrentGame.Observers.Values.ToList<ICharacterInfo>();
            }

            Log.LogMsg("Player [" + note.TargetPlayer != null ? note.TargetPlayer.CharacterName : "Some Player" + "] STOPPED OBSERVING game [" + note.TheGameID + "]");
            FireObserverRemoved(note.ReplyMessage, this, note.TargetPlayer as CharacterInfo, note.TheGame as Game, note.Parms);
            
            if (Client.Instance.User != null && Client.Instance.User.CurrentCharacter != null)
            {
                if (Client.Instance.User.CurrentCharacter.ID == note.TargetPlayer.ID) // did I get removed?
                {
                    // yes, i was removed from the game. we are no longer part of a game.
                    CurrentGame = null;
                }
            }
        }

        public delegate void OnBeforeLoginRequestDelegate(INetworkConnection con, PacketLoginRequest login);

        #region BeforeLoginRequest Event
        private OnBeforeLoginRequestDelegate BeforeLoginRequestInvoker;

        /// <summary>
        /// Fires after we connect to the content server and try to instantiate our game instance
        /// </summary>
        public event OnBeforeLoginRequestDelegate BeforeLoginRequest
        {
            add
            {
                AddHandler_BeforeLoginRequest(value);
            }
            remove
            {
                RemoveHandler_BeforeLoginRequest(value);
            }
        }


        private void AddHandler_BeforeLoginRequest(OnBeforeLoginRequestDelegate value)
        {
            BeforeLoginRequestInvoker = (OnBeforeLoginRequestDelegate)Delegate.Combine(BeforeLoginRequestInvoker, value);
        }

        private void RemoveHandler_BeforeLoginRequest(OnBeforeLoginRequestDelegate value)
        {
            BeforeLoginRequestInvoker = (OnBeforeLoginRequestDelegate)Delegate.Remove(ContentCreateGameRequestResolvedInvoker, value);
        }

        private void FireOnBeforeLoginRequest(INetworkConnection con, PacketLoginRequest login)
        {
            if (BeforeLoginRequestInvoker != null)
            {
                BeforeLoginRequestInvoker(con, login);
            }
        }
        #endregion

        protected override void OnBeforeLoginRequest(PacketLoginRequest req)
        {
            // Add our game creation/join parms to the login packet.
            base.OnBeforeLoginRequest(req);
            FireOnBeforeLoginRequest(this, req);
        }

        protected override void OnSocketKilled(string msg)
        {
            CurrentGame = null;
            base.OnSocketKilled(msg);
        }

        protected override void HandlePacket(Packet p)
        {
            // is the packet a GameMessage or a reply to a GameMessage, then the game will handle it.
            if (p.PacketTypeID == (int)LobbyPacketType.GameMessage)
            {
                try
                {
                    if (CurrentGame != null)
                    {
                        CurrentGame.HandleGamePacket(this, p as PacketGameMessage);
                    }
                }
                catch
                { 
                }
            }
            else if (p.PacketTypeID == (int)PacketType.GenericReply && ((PacketReply)p).ReplyPacketType == (int)LobbyPacketType.GameMessage)
            {
                try
                {
                    if (CurrentGame != null)
                    {
                        CurrentGame.HandleGamePacketReply(this, p as PacketGameMessage);
                    }
                }
                catch
                {
                }
            }
            else
            {
                base.HandlePacket(p);
            }
        }




    }
}
