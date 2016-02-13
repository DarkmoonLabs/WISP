using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Shared;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace Shared
{
    /// <summary>
    /// A game client object to communicate with lobby gaming server clusters
    /// </summary>
    public class LobbyClient : Client
    {
        public LobbyClient() : base()
        {            
            Factory.Instance.Register(typeof(Game), () => { return new Game(); });
            m_Instance = this;
            m_EmptyGame = new ClientGame(new Game(), this, null);
        }

        /// <summary>
        /// Reference game object
        /// </summary>
        private ClientGame m_EmptyGame;

        #region Events
        
        #region CompleteMatchListingArrived Event
        private Action<bool, string, LobbyClient> CompleteMatchListingArrivedInvoker;

        /// <summary>
        /// Fires from Central Server Connection.  Contains information on all matches for this cluster.
        /// </summary>
        public event Action<bool, string, LobbyClient> CompleteMatchListingArrived
        {
            add
            {
                AddHandler_CompleteMatchListingArrived(value);
            }
            remove
            {
                RemoveHandler_CompleteMatchListingArrived(value);
            }
        }

        
        private void AddHandler_CompleteMatchListingArrived(Action<bool, string, LobbyClient> value)
        {
            CompleteMatchListingArrivedInvoker = (Action<bool, string, LobbyClient>)Delegate.Combine(CompleteMatchListingArrivedInvoker, value);
        }

        
        private void RemoveHandler_CompleteMatchListingArrived(Action<bool, string, LobbyClient> value)
        {
            CompleteMatchListingArrivedInvoker = (Action<bool, string, LobbyClient>)Delegate.Remove(CompleteMatchListingArrivedInvoker, value);
        }

        private void FireCompleteMatchListingArrived(bool result, string msg, LobbyClient sender)
        {
            if (CompleteMatchListingArrivedInvoker != null)
            {
                CompleteMatchListingArrivedInvoker(result, msg, sender);
            }
        }
        #endregion
      
        #region CentralGameCreationRequestResolved Event
        private CreateGameRequestResolvedDelegate CentralGameCreationRequestResolvedInvoker;

        /// <summary>
        /// Game creation requests are posed to the Central Server.  Central surveys the cluster and determines an appropriate Content Server. If all
        /// Content Servers are at capacity, or none are online, Central will fire this event and indicate the reason for the failure.  If Central has found
        /// an appropriate Content Server, this will fire with a Success=True.  Player is then transferred to the target Content Server.  Once on the Content
        /// Server, the match/game/room is actually created.  
        /// </summary>
        public event CreateGameRequestResolvedDelegate CentralGameCreationRequestResolved
        {
            add
            {
                AddHandler_CentralGameCreationRequestResolved(value);
            }
            remove
            {
                RemoveHandler_CentralGameCreationRequestResolved(value);
            }
        }

        
        private void AddHandler_CentralGameCreationRequestResolved(CreateGameRequestResolvedDelegate value)
        {
            CentralGameCreationRequestResolvedInvoker = (CreateGameRequestResolvedDelegate)Delegate.Combine(CentralGameCreationRequestResolvedInvoker, value);
        }

        
        private void RemoveHandler_CentralGameCreationRequestResolved(CreateGameRequestResolvedDelegate value)
        {
            CentralGameCreationRequestResolvedInvoker = (CreateGameRequestResolvedDelegate)Delegate.Remove(CentralGameCreationRequestResolvedInvoker, value);
        }

        private void FireCentralGameCreationRequestResolved(bool result, string msg, ClientServerOutboundConnection con, Game g)
        {
            if (CentralGameCreationRequestResolvedInvoker != null)
            {
                CentralGameCreationRequestResolvedInvoker(result, msg, con, g);
            }
        }
        #endregion

        #region ContentGameCreationRequestResolved Event
        private CreateGameRequestResolvedDelegate ContentGameCreationRequestResolvedInvoker;

        /// <summary>
        /// Game creation requests initiate on the Central Server.  Central surveys the cluster and determines an appropriate Content Server. If all
        /// Content Servers are at capacity, or none are online, Central will fire this event and indicate the reason for the failure.  If Central has found
        /// an appropriate Content Server.  Player is then transferred to the target Content Server.  Once on the Content
        /// Server, the match/game/room is actually instantiated.  If Match creation fails on the content server, this event will fire with the appropriate parameters set. If Match creation
        /// fails on the Content Server, the player is directed back to the Central Server.  If Match creation succeeds on the Content Server, this event will fire
        /// with a Success=True.
        /// </summary>
        public event CreateGameRequestResolvedDelegate ContentGameCreationRequestResolved
        {
            add
            {
                AddHandler_ContentGameCreationRequestResolved(value);
            }
            remove
            {
                RemoveHandler_ContentGameCreationRequestResolved(value);
            }
        }

        
        private void AddHandler_ContentGameCreationRequestResolved(CreateGameRequestResolvedDelegate value)
        {
            ContentGameCreationRequestResolvedInvoker = (CreateGameRequestResolvedDelegate)Delegate.Combine(ContentGameCreationRequestResolvedInvoker, value);
        }

        
        private void RemoveHandler_ContentGameCreationRequestResolved(CreateGameRequestResolvedDelegate value)
        {
            ContentGameCreationRequestResolvedInvoker = (CreateGameRequestResolvedDelegate)Delegate.Remove(ContentGameCreationRequestResolvedInvoker, value);
        }

        private void FireContentGameCreationRequestResolved(bool result, string msg, ClientServerOutboundConnection con, Game g)
        {
            if (ContentGameCreationRequestResolvedInvoker != null)
            {
                ContentGameCreationRequestResolvedInvoker(result, msg, con, g);
            }
        }
        #endregion

        #region CentralServerJoinGameResolved Event
        private JoinGameResultDelegate CentralServerJoinGameResolvedInvoker;

        /// <summary>
        /// Fires on central server after we make a game join request.  Lets us know if the join was authorized by central or not.  Once the join is authorized
        /// by central, we get transferred to the content server.  Once on the content server we try to hook into the actual game (another join request).  At that point,
        /// GameServerPlayerAdded will fire.
        /// </summary>
        public event JoinGameResultDelegate CentralServerJoinGameResolved
        {
            add
            {
                AddHandler_CentralServerJoinGameResolved(value);
            }
            remove
            {
                RemoveHandler_CentralServerJoinGameResolved(value);
            }
        }

        
        private void AddHandler_CentralServerJoinGameResolved(JoinGameResultDelegate value)
        {
            CentralServerJoinGameResolvedInvoker = (JoinGameResultDelegate)Delegate.Combine(CentralServerJoinGameResolvedInvoker, value);
        }

        
        private void RemoveHandler_CentralServerJoinGameResolved(JoinGameResultDelegate value)
        {
            CentralServerJoinGameResolvedInvoker = (JoinGameResultDelegate)Delegate.Remove(CentralServerJoinGameResolvedInvoker, value);
        }

        private void FireCentralServerJoinGameResolved(bool result, string msg, ClientServerOutboundConnection con, Game g)
        {
            if (CentralServerJoinGameResolvedInvoker != null)
            {
                CentralServerJoinGameResolvedInvoker(result, msg, con, g);
            }
        }
        #endregion

        #region GameServerJoinGameResolved Event
        private JoinGameResultDelegate GameServerJoinGameResolvedInvoker;

        /// <summary>
        /// Fires on game server after we connect to the content (game) server and request connection to a specific a game.  
        /// </summary>
        public event JoinGameResultDelegate GameServerJoinGameResolved
        {
            add
            {
                AddHandler_GameServerJoinGameResolved(value);
            }
            remove
            {
                RemoveHandler_GameServerJoinGameResolved(value);
            }
        }

        
        private void AddHandler_GameServerJoinGameResolved(JoinGameResultDelegate value)
        {
            GameServerJoinGameResolvedInvoker = (JoinGameResultDelegate)Delegate.Combine(GameServerJoinGameResolvedInvoker, value);
        }

        
        private void RemoveHandler_GameServerJoinGameResolved(JoinGameResultDelegate value)
        {
            GameServerJoinGameResolvedInvoker = (JoinGameResultDelegate)Delegate.Remove(GameServerJoinGameResolvedInvoker, value);
        }

        private void FireGameServerJoinGameResolved(bool result, string msg, ClientServerOutboundConnection con, Game g)
        {
            if (GameServerJoinGameResolvedInvoker != null)
            {
                GameServerJoinGameResolvedInvoker(result, msg, con, g);
            }
        }
        #endregion
             
        #region PlayerJoinedGame Event
        private PlayerJoinedDelegate PlayerJoinedGameInvoker;
        /// <summary>
        /// Fires when the Content Server notifies us that a player has joined the game. This event will also fire on the content (game) server if
        /// we are connecting to it and are not able to join.  The ReplyCode will read Failure on this event if the latter is the case.
        /// </summary>
        public event PlayerJoinedDelegate PlayerJoinedGame
        {
            add
            {
                AddHandler_PlayerJoinedGame(value);
            }
            remove
            {
                RemoveHandler_PlayerJoinedGame(value);
            }
        }

        
        private void AddHandler_PlayerJoinedGame(PlayerJoinedDelegate value)
        {
            PlayerJoinedGameInvoker = (PlayerJoinedDelegate)Delegate.Combine(PlayerJoinedGameInvoker, value);
        }

        
        private void RemoveHandler_PlayerJoinedGame(PlayerJoinedDelegate value)
        {
            PlayerJoinedGameInvoker = (PlayerJoinedDelegate)Delegate.Remove(PlayerJoinedGameInvoker, value);
        }

        private void FirePlayerJoinedGame(bool isFromCentral, bool isFromContent, bool result, string msg, ClientServerOutboundConnection con, CharacterInfo player, Game g, PropertyBag parms)
        {
            if (PlayerJoinedGameInvoker != null)
            {
                PlayerJoinedGameInvoker(isFromCentral, isFromContent, result, msg, con, player, g, parms);
            }
        }
        #endregion
       
        #region ObserverJoinedGame Event
        private PlayerJoinedDelegate ObserverJoinedGameInvoker;

        /// <summary>
        /// Fires when the Content Server notifies us that a player has joined to observe the game.
        /// </summary>
        public event PlayerJoinedDelegate ObserverJoinedGame
        {
            add
            {
                AddHandler_ObserverJoinedGame(value);
            }
            remove
            {
                RemoveHandler_ObserverJoinedGame(value);
            }
        }

        
        private void AddHandler_ObserverJoinedGame(PlayerJoinedDelegate value)
        {
            ObserverJoinedGameInvoker = (PlayerJoinedDelegate)Delegate.Combine(ObserverJoinedGameInvoker, value);
        }

        
        private void RemoveHandler_ObserverJoinedGame(PlayerJoinedDelegate value)
        {
            ObserverJoinedGameInvoker = (PlayerJoinedDelegate)Delegate.Remove(ObserverJoinedGameInvoker, value);
        }

        private void FireObserverJoinedGame(bool isFromCentral, bool isFromContent, bool result, string msg, ClientServerOutboundConnection con, CharacterInfo player, Game g, PropertyBag parms)
        {
            if (ObserverJoinedGameInvoker != null)
            {
                ObserverJoinedGameInvoker(isFromCentral, isFromContent, result, msg, con, player, g, parms);
            }
        }
        #endregion

        #region PlayerRemovedFromGame Event
        private PlayerRemovedDelegate PlayerRemovedFromGameInvoker;

        /// <summary>
        /// Fires when Content Server notifies us that a player has left the game.
        /// </summary>
        public event PlayerRemovedDelegate PlayerRemovedFromGame
        {
            add
            {
                AddHandler_PlayerRemovedFromGame(value);
            }
            remove
            {
                RemoveHandler_PlayerRemovedFromGame(value);
            }
        }

        
        private void AddHandler_PlayerRemovedFromGame(PlayerRemovedDelegate value)
        {
            PlayerRemovedFromGameInvoker = (PlayerRemovedDelegate)Delegate.Combine(PlayerRemovedFromGameInvoker, value);
        }

        
        private void RemoveHandler_PlayerRemovedFromGame(PlayerRemovedDelegate value)
        {
            PlayerRemovedFromGameInvoker = (PlayerRemovedDelegate)Delegate.Remove(PlayerRemovedFromGameInvoker, value);
        }

        private void FirePlayerRemovedFromGame(string msg, ClientServerOutboundConnection con, CharacterInfo player, Game g, PropertyBag parms)
        {
            if (PlayerRemovedFromGameInvoker != null)
            {
                PlayerRemovedFromGameInvoker(msg, con, player, g, parms);
            }
        }
        #endregion
       
        #region ObserverRemovedFromGame Event
        private PlayerRemovedDelegate ObserverRemovedFromGameInvoker;

        /// <summary>
        /// Fires when Content Server notifies us that an observer has stopped observing the game.
        /// </summary>
        public event PlayerRemovedDelegate ObserverRemovedFromGame
        {
            add
            {
                AddHandler_ObserverRemovedFromGame(value);
            }
            remove
            {
                RemoveHandler_ObserverRemovedFromGame(value);
            }
        }

        
        private void AddHandler_ObserverRemovedFromGame(PlayerRemovedDelegate value)
        {
            ObserverRemovedFromGameInvoker = (PlayerRemovedDelegate)Delegate.Combine(ObserverRemovedFromGameInvoker, value);
        }

        
        private void RemoveHandler_ObserverRemovedFromGame(PlayerRemovedDelegate value)
        {
            ObserverRemovedFromGameInvoker = (PlayerRemovedDelegate)Delegate.Remove(ObserverRemovedFromGameInvoker, value);
        }

        private void FireObserverRemovedFromGame(string msg, ClientServerOutboundConnection con, CharacterInfo player, Game g, PropertyBag parms)
        {
            if (ObserverRemovedFromGameInvoker != null)
            {
                ObserverRemovedFromGameInvoker(msg, con, player, g, parms);
            }
        }
        #endregion

        #region GameStartReply Event
        private GameStartReplyDelegate GameStartReplyInvoker;

        /// <summary>
        /// Fires when we receive a reply to our manual request to start a game.
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
        /// Fires when the game is ended one way or another
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

        #region QuickMatchResultArrived Event
        private Action<bool, string, LobbyClient> QuickMatchResultArrivedInvoker;

        /// <summary>
        /// Fires from Central Server Connection.  Contains information on the quick match result
        /// </summary>
        public event Action<bool, string, LobbyClient> QuickMatchResultArrived
        {
            add
            {
                AddHandler_QuickMatchResultArrived(value);
            }
            remove
            {
                RemoveHandler_QuickMatchResultArrived(value);
            }
        }

        private void AddHandler_QuickMatchResultArrived(Action<bool, string, LobbyClient> value)
        {
            QuickMatchResultArrivedInvoker = (Action<bool, string, LobbyClient>)Delegate.Combine(QuickMatchResultArrivedInvoker, value);
        }

        private void RemoveHandler_QuickMatchResultArrived(Action<bool, string, LobbyClient> value)
        {
            QuickMatchResultArrivedInvoker = (Action<bool, string, LobbyClient>)Delegate.Remove(QuickMatchResultArrivedInvoker, value);
        }

        private void FireQuickMatchResultArrived(bool result, string msg, LobbyClient sender)
        {
            if (QuickMatchResultArrivedInvoker != null)
            {
                QuickMatchResultArrivedInvoker(result, msg, sender);
            }
        }
        #endregion


        #endregion

        #region Data

        private int m_TotalGamesInLobby;
        /// <summary>
        /// Total number of games on the lobby cluster.
        /// </summary>
        public int TotalGamesInLobby
        {
            get { return m_TotalGamesInLobby; }
            set { m_TotalGamesInLobby = value; }
        }
        
        private List<Game> m_Games = new List<Game>();
        /// <summary>
        /// A list of all available games that this client knows about.  This list is refreshed as a result of LobbyClient.RequestGameList()
        /// </summary>
        public List<Game> Games
        {
            get { return m_Games; }
            set { m_Games = value; }
        }

        private Game m_QuickMatchGame;
        /// <summary>
        /// The result of a quick match query
        /// </summary>
        public Game QuickMatchGame
        {
            get { return m_QuickMatchGame; }
            set { m_QuickMatchGame = value; }
        }
        

        /// <summary>
        /// m_GameServer server cast to the appropriate type
        /// </summary>
        protected LobbyClientGameServerOutboundConnection LobbyGameServer
        {
            get
            {
                try
                {
                    return m_GameServer as LobbyClientGameServerOutboundConnection;
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// m_Central server cast to the appropriate type
        /// </summary>
        protected LobbyClientCentralServerOutboundConnection LobbyCentralServer
        {
            get
            {
                try
                {
                    return m_CentralServer as LobbyClientCentralServerOutboundConnection;
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// The current game we are connected to, or null if none.  This is only set if we are actively participating in a game,
        /// not while trying to connect or create one.
        /// </summary>
        public ClientGame CurrentGame
        {
            get
            {
                LobbyClientGameServerOutboundConnection gs = LobbyGameServer;
                ClientGame cg = null;
                if (gs != null && gs.IsAlive)
                {
                    cg = gs.CurrentGame;
                }

                if (cg == null)
                {
                    return m_EmptyGame;
                }

                return cg;
            }
        }

        /// <summary>
        /// Stores game creation/join parms during the creation/join phase (which is a multi-step asynch process)
        /// </summary>
        public PropertyBag TargetGameOptions = new PropertyBag();
        
        /// <summary>
        /// Contains a reference to the last game we attempted to join/create.  Values may be out of date, depending on how long ago the join
        /// occured.  Use LobbyClient.CurrentGame for a more up-to-date version of the data, if we're currently participating in the game.
        /// The version of this data is what we got from central server when we originally asked to join/create the game.  LobbyClient.CurrentGame
        /// is the actual data of the game once we connect to the game server.
        /// </summary>
        protected Game m_PendingGameToJoin = null;

        private static LobbyClient m_Instance;
        /// <summary>
        /// Singleton instance.
        /// </summary>
        public static LobbyClient Instance
        {
            get
            {
                if (m_Instance == null)
                {                    
                    m_Instance = new LobbyClient();
                   //Log.LogMsg("*** CREATING NEW LOBBYCLIENT OBJECT " + m_Instance.GetHashCode().ToString() + " ***");
                }

                //Log.LogMsg("*** RETURNING LOBBYCLIENT OBJECT " + m_Instance.GetHashCode().ToString() + " ***");
                return m_Instance;
            }
        }
        #endregion

        #region Public Interface

        #region GameRequestSearch Event
        private GameRequestSearchDelegate GameRequestSearchInvoker;
        public delegate PropertyBag GameRequestSearchDelegate();
        /// <summary>
        /// Fires when the LobbyClient is about to submit a request for a game listing  Use this event to add additional search parameters that might be relevant for your implementation.
        /// </summary>
        public event GameRequestSearchDelegate GameRequestSearch
        {
            add
            {
                AddHandler_GameRequestSearch(value);
            }
            remove
            {
                RemoveHandler_GameRequestSearch(value);
            }
        }

        
        private void AddHandler_GameRequestSearch(GameRequestSearchDelegate value)
        {
            GameRequestSearchInvoker = (GameRequestSearchDelegate)Delegate.Combine(GameRequestSearchInvoker, value);
        }

        
        private void RemoveHandler_GameRequestSearch(GameRequestSearchDelegate value)
        {
            GameRequestSearchInvoker = (GameRequestSearchDelegate)Delegate.Remove(GameRequestSearchInvoker, value);
        }

        private PropertyBag FireGameRequestSearch()
        {
            if (GameRequestSearchInvoker != null)
            {
                return GameRequestSearchInvoker();
            }

            return new PropertyBag();
        }
        #endregion

        /// <summary>
        /// Request a list of all games that the server knows about. This is a non-blocking call. Once the game data arrives, LobbyClient.Games 
        /// will have the latest data in it and LobbyClient.CompleteMatchListingArrived event will fire.
        /// </summary>
        /// <param name="msg">a string which will hold player facing text message if the call fails (returns false)</param>
        /// <returns>true if IsGameServerConnected and was ReadyForPlay and the request was thus sent</returns>
        public bool RequestGameListing(ref string msg, int page, int numPerPage, bool includeInProgress, int minPlayersAllowed, PropertyBag parms)
        {
            msg = "";
            if (!IsCentralServerConnected || !CentralReadyForCommunication)
            {
                msg = "Not ready to communicate with game server.";
                return false;
            }

            Log.LogMsg("Requesting content listing from game server.");
            PropertyBag bag = new PropertyBag();
            bag.SetProperty("Page", page);
            bag.SetProperty("NumPerPage", numPerPage);
            bag.SetProperty("IncludeInProgress", includeInProgress);
            bag.SetProperty("MinPlayersAllowed", minPlayersAllowed);
            
            // ad-hoc parms
            bag.UpdateWithValues(parms);

            if (GameRequestSearchInvoker != null)
            {
                PropertyBag add = FireGameRequestSearch();
                if (add != null)
                {
                    bag.UpdateWithValues(add);
                }
            }

            m_CentralServer.SendGenericMessage((int)GenericLobbyMessageType.RequestGameListing, bag, false);
            return true;
        }

        /// <summary>
        /// Request the creation of a new game instance on the game server.  Any pertinent options should be
        /// included in the parameter.  This is a non-blocking call.  Once the game creation request has been resolved
        /// the LobbyClient.GameCreationRequestResolved event will fire.
        /// </summary>
        /// <param name="options">any options required by the server for game creation</param>
        /// <param name="msg">a string which will hold player facing text message if the call fails (returns false)</param>
        /// <returns>true if IsGameServerConnected and was ReadyForPlay and the request was thus sent</returns>
        public bool RequestCreateNewGame(PropertyBag options, ref string msg)
        {
            msg = "";
            if (!IsCentralServerConnected || !CentralReadyForCommunication)
            {
                msg = "Not ready to communicate with game server.";
                return false;
            }

            Log.LogMsg("Requesting game [" + options.GetStringProperty((int)PropertyID.Name) + "] creation from Central server.");
            options.SetProperty("IsNewGame", true);
            TargetGameOptions = options;
            m_CentralServer.SendGenericMessage((int)GenericLobbyMessageType.CreateGame, options, true); // encrypt it, beccause it could contain a password
            return true;
        }

        /// <summary>
        /// Request a quick game match-up from central server. Server will attempt to make a match based on server criteria.
        /// </summary>
        /// <param name="options">any options required by the server for game creation</param>
        /// <param name="msg">a string which will hold player facing text message if the call fails (returns false)</param>
        /// <returns>true if IsGameServerConnected and was ReadyForPlay and the request was thus sent</returns>
        public bool RequestQuickMatch(PropertyBag options, ref string msg)
        {
            msg = "";
            if (!IsCentralServerConnected || !CentralReadyForCommunication)
            {
                msg = "Not ready to communicate with game server.";
                return false;
            }

            Log.LogMsg("Requesting quick game match-up from Central server.");
            TargetGameOptions = options;
            m_CentralServer.SendGenericMessage((int)GenericLobbyMessageType.RequestQuickMatch, options, false); 
            return true;
        }

        /// <summary>
        /// Request the joining of an existing game instance on the game server.  
        /// This is a non-blocking call.  Once the game join request has been resolved
        /// the LobbyClient.GameJoinRequestResolved event will fire.
        /// </summary>
        /// <param name="gameId">the ID of the game we wish to join</param>
        /// <param name="msg">a string which will hold player facing text message if the call fails (returns false)</param>
        /// <returns>true if IsGameServerConnected and was ReadyForPlay and the request was thus sent</returns>
        public bool RequestJoinGame(Guid gameId, ref string msg)
        {
            return RequestJoinGame(gameId, ref msg, null);
        }

        /// <summary>
        /// Request the joining of an existing game instance on the game server.  
        /// This is a non-blocking call.  Once the game join request has been resolved
        /// the LobbyClient.GameJoinRequestResolved event will fire.
        /// </summary>
        /// <param name="gameId">the ID of the game we wish to join</param>
        /// <param name="msg">a string which will hold player facing text message if the call fails (returns false)</param>
        /// <returns>true if IsGameServerConnected and was ReadyForPlay and the request was thus sent</returns>
        public bool RequestJoinGame(Guid gameId, ref string msg, PropertyBag parms)
        {
            msg = "";
            if (!IsCentralServerConnected || !CentralReadyForCommunication)
            {
                msg = "Not ready to communicate with game server.";
                return false;
            }

            PropertyBag options = new PropertyBag();
            options.SetProperty("IsNewGame", false);
            options.SetProperty((int)PropertyID.GameId, gameId);
            if (parms != null)
            {
                options.UpdateWithValues(parms);
            }
            TargetGameOptions = options;
            Log.LogMsg("Requesting to join game " + gameId.ToString());
            m_CentralServer.SendGenericMessage((int)GenericLobbyMessageType.JoinGame, options, false);
            return true;
        }

        #endregion

        #region Central Server Event Handlers

        private void OnCentralServer_CompleteMatchListingArrived(bool result, string msg, LobbyClientCentralServerOutboundConnection sender, List<Game> games, int totalGames)
        {
            m_TotalGamesInLobby = totalGames;
            m_Games.Clear();
            if (result)
            {
                for (int i = 0; i < games.Count; i++)
                {
                    m_Games.Add(games[i] as Game);
                }
            }

            FireCompleteMatchListingArrived(result, msg, this);
        }

        private void OnCentralServer_CreateGameRequestResolved(bool success, string msg, ClientServerOutboundConnection sender, Game newGame)
        {
            if (success && newGame != null)
            {
                m_PendingGameToJoin = newGame;
            }

            FireCentralGameCreationRequestResolved(success, msg, sender, newGame);
        }


        private void OnGameServer_CreateGameRequestResolved(bool success, string msg, ClientServerOutboundConnection sender, Game newGame)
        {
            FireContentGameCreationRequestResolved(success, msg, sender, newGame);
        }

        private void OnCentralServer_PlayerJoined(bool isFromCentralServer, bool isFromContentServer, bool success, string msg, ClientServerOutboundConnection sender, CharacterInfo player, Game theGame, PropertyBag parms)
        {
            if (theGame == null)
            {
                msg += "Unknow game: null";
                success = false;
            }

            if (success)
            {
                m_PendingGameToJoin = theGame;
            }

            FirePlayerJoinedGame(true, false, success, msg, sender, player, theGame, parms);

            // let's disconnect from central and join the GS, if Central says it's okay.
            if (success)
            {
                Log.LogMsg("Successfully joined a game. Waiting for transfer confirmation. ");
            }
            else
            {
                Log.LogMsg("Failed to join content instance. " + msg);
            }
        }

        private void OnCentralServer_SocketKilled(object sender, string msg)
        {
            LobbyClientCentralServerOutboundConnection con = sender as LobbyClientCentralServerOutboundConnection;
            con.SocketKilled -= new SocketKilledDelegate(OnCentralServer_SocketKilled);
            con.CompleteMatchListingArrived -= new CompleteGameListingArrivedDelegate(OnCentralServer_CompleteMatchListingArrived);    
            con.CentralCreateGameRequestResolved -= new CreateGameRequestResolvedDelegate(OnCentralServer_CreateGameRequestResolved);
            con.JoinGameResolved -= new JoinGameResultDelegate(OnCentralServer_JoinResult);
            con.QuickMatchResultArrived -= new QuickMatchResultArrivedDelegate(OnCentralServer_QuickMatchResultArrived);
        }
        
        #endregion

        #region Game Server Event Handlers

        private void OnGameServer_PlayerRemoved(string msg, ClientServerOutboundConnection sender, CharacterInfo player, Game theGame, PropertyBag parms)
        {
            FirePlayerRemovedFromGame(msg, sender, player, theGame, parms);
        }

        private void OnGameServer_PlayerJoined(bool isFromCentralServer, bool isFromContentServer, bool success, string msg, ClientServerOutboundConnection sender, CharacterInfo player, Game theGame, PropertyBag parms)
        {            
            FirePlayerJoinedGame(false, true, success, msg, sender, player, theGame, parms);
        }

        private void OnGameServer_SocketKilled(object sender, string msg)
        {
            //base.OnGameServer_SocketKilled(sender, msg);
            LobbyClientGameServerOutboundConnection con = sender as LobbyClientGameServerOutboundConnection;
            con.BeforeLoginRequest -= new LobbyClientGameServerOutboundConnection.OnBeforeLoginRequestDelegate(OnGameServer_OnBeforeLoginRequest);
            con.SocketKilled -= new SocketKilledDelegate(OnGameServer_SocketKilled);
            con.PlayerJoined -= new PlayerJoinedDelegate(OnGameServer_PlayerJoined);
            con.PlayerRemoved -= new PlayerRemovedDelegate(OnGameServer_PlayerRemoved);
            con.ContentCreateGameRequestResolved -= new CreateGameRequestResolvedDelegate(OnGameServer_CreateGameRequestResolved);
            con.GameStartReply -= new GameStartReplyDelegate(OnGameServer_GameStartReply);
            con.ObserverJoined -= new PlayerJoinedDelegate(OnGameServer_ObserverJoined);
            con.ObserverRemoved -= new PlayerRemovedDelegate(OnGameServer_ObserverRemoved);
            con.GameEnded -= new GameEndDelegate(OnGameServer_GameEnded);
            con.JoinGameResolved -= new JoinGameResultDelegate(OnGameServer_JoinResult);
        }

        void OnGameServer_JoinResult(bool result, string msg, ClientServerOutboundConnection sender, Game g)
        {
            FireGameServerJoinGameResolved(result, msg, sender, g);
        }

        #endregion

        #region Boilerplate

        protected override ClientCentralServerOutboundConnection OnCentralServerConnectionCreate(bool isBlocking)
        {
            LobbyClientCentralServerOutboundConnection con = new LobbyClientCentralServerOutboundConnection(isBlocking);
            con.SocketKilled += new SocketKilledDelegate(OnCentralServer_SocketKilled);
            con.CompleteMatchListingArrived += new CompleteGameListingArrivedDelegate(OnCentralServer_CompleteMatchListingArrived);
            con.CentralCreateGameRequestResolved += new CreateGameRequestResolvedDelegate(OnCentralServer_CreateGameRequestResolved);
            con.JoinGameResolved += new JoinGameResultDelegate(OnCentralServer_JoinResult);
            con.QuickMatchResultArrived += new QuickMatchResultArrivedDelegate(OnCentralServer_QuickMatchResultArrived);
            return con;
        }

        void OnCentralServer_QuickMatchResultArrived(bool result, string msg, LobbyClientCentralServerOutboundConnection sender, Game g)
        {
            m_QuickMatchGame = g;
            FireQuickMatchResultArrived(result, msg, this);
        }

        void OnCentralServer_JoinResult(bool result, string msg, ClientServerOutboundConnection sender, Game g)
        {
            FireCentralServerJoinGameResolved(result, msg, sender, g);
        }

        /// <summary>
        /// Gets called when the base LobbyClient creates the CurrentGame object using the base game data
        /// that was received from the Central server.  If you want to use a derivative Game object instead of the
        /// standard ClientGame type, instantiate it here and return it.
        /// </summary>
        /// <param name="baseData"></param>
        /// <returns></returns>
        protected virtual ClientGame OnCreateCurrentGameObject(Game baseData)
        {
            return new ClientGame(baseData, this, m_GameServer);
        }

        protected override ClientGameServerOutboundConnection OnGameServerConnectionCreate(bool isBlocking)
        {            
            LobbyClientGameServerOutboundConnection con = new LobbyClientGameServerOutboundConnection(isBlocking);            
            return con;
        }

        void OnGameServer_OnBeforeLoginRequest(INetworkConnection con, PacketLoginRequest login)
        {
            Log.LogMsg("Appending target game options [" + TargetGameOptions.PropertyCount.ToString() + " properties] to login request. Login parms count before append is [" + login.Parms.PropertyCount.ToString() + "].");
            login.Parms.UpdateWithValues(TargetGameOptions);
            Log.LogMsg("Login parms count after append is [" + login.Parms.PropertyCount.ToString() + "].");
        }

        void OnGameServer_GameStartReply(LobbyClientGameServerOutboundConnection con, bool result, string msg)
        {
            Log.LogMsg("Game start request [" + (result? "SUCCEEDED: " : "FAILED: ") + msg + "].");
            FireGameStartReply(con, result, msg);
        }

        protected override void OnAfterGameServerConnectionCreate(ClientGameServerOutboundConnection con)
        {
            base.OnAfterGameServerConnectionCreate(con);
            LobbyClientGameServerOutboundConnection lcon = con as LobbyClientGameServerOutboundConnection;
            
            lcon.ContentCreateGameRequestResolved += new CreateGameRequestResolvedDelegate(OnGameServer_CreateGameRequestResolved);
            lcon.SocketKilled += new SocketKilledDelegate(OnGameServer_SocketKilled);
            lcon.PlayerJoined += new PlayerJoinedDelegate(OnGameServer_PlayerJoined);
            lcon.ObserverJoined += new PlayerJoinedDelegate(OnGameServer_ObserverJoined);
            lcon.PlayerRemoved += new PlayerRemovedDelegate(OnGameServer_PlayerRemoved);
            lcon.ObserverRemoved += new PlayerRemovedDelegate(OnGameServer_ObserverRemoved);
            lcon.GameStartReply += new GameStartReplyDelegate(OnGameServer_GameStartReply);
            lcon.GameEnded += new GameEndDelegate(OnGameServer_GameEnded);
            lcon.JoinGameResolved += new JoinGameResultDelegate(OnGameServer_JoinResult);
            lcon.GameActivating += new LobbyClientGameServerOutboundConnection.GameActivatingDelegate(OnGameServer_GameActivating);

            lcon.BeforeLoginRequest += new LobbyClientGameServerOutboundConnection.OnBeforeLoginRequestDelegate(OnGameServer_OnBeforeLoginRequest);
        }

        ClientGame OnGameServer_GameActivating(LobbyClientGameServerOutboundConnection con, Game gameData)
        {
            return OnCreateCurrentGameObject(gameData);
        }

        void OnGameServer_GameEnded(LobbyClientGameServerOutboundConnection con, PropertyBag finalProperties, string msg)
        {
            Log.LogMsg(">>> Game ended <<<");
            FireGameEnded(con, finalProperties, msg);
        }

        void OnGameServer_ObserverRemoved(string msg, ClientServerOutboundConnection sender, CharacterInfo player, Game theGame, PropertyBag parms)
        {
            FireObserverRemovedFromGame(msg, sender, player, theGame, parms);
        }

        void OnGameServer_ObserverJoined(bool isFromCentralServer, bool isFromContentServer, bool success, string msg, ClientServerOutboundConnection sender, CharacterInfo player, Game theGame, PropertyBag parms)
        {
            FireObserverJoinedGame(false, true, success, msg, sender, player, theGame, parms);
        }

        protected override LoginServerOutboundConnection OnLoginServerConnectionCreate(bool isBlocking)
        {
            return base.OnLoginServerConnectionCreate(isBlocking);
        }

        #endregion

    }
}
