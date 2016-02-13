using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Shared
{
    public class GameManager
    {
        /// <summary>
        /// Fires when we are asked to create a new game on this server.  Return false to prevent creation.
        /// </summary>
        public event Func<GameServerGame, bool> GameCreationRequest;

        /// <summary>
        /// Fires when we have created a new game on this server.
        /// </summary>
        public event Action<GameServerGame> GameCreated;


        public GameManager()
        {
            ConnectionManager.RequestConnectionCount += new ConnectionManager.RequestConnectionCountDelegate(ConnectionManager_RequestConnectionCount);
            Games = new Dictionary<Guid, GameServerGame>();
        }


        private int ConnectionManager_RequestConnectionCount(int actualConnections)
        {
            return ConnectionManager.ParentConnections.Count + m_TotalProjectedPlayerCount;
        }

        #region Data
        
        /// <summary>
        /// The number of projected players, which is a count of MaxPlayers + MaxObservers for every game.
        /// </summary>
        private int m_TotalProjectedPlayerCount = 0;

        /// <summary>
        /// synch root for thread-safe data manipulation from matches
        /// </summary>
        private object MatchDataLock = new object();

        /// <summary>
        /// All of the games on this server
        /// </summary>
        public Dictionary<Guid, GameServerGame> Games { get; set; }

        /// <summary>
        /// All games known about on this cluster. in a list.
        /// </summary>
        public List<GameServerGame> AllGames
        {
            get 
            {
                lock (MatchDataLock)
                {
                    return (List<GameServerGame>)Games.Values.ToList<GameServerGame>();
                }
            }        
        }
        
        private static GameManager m_Instance;
        public  GSInboundServerConnection CentralServer;
        /// <summary>
        /// Singleton instance
        /// </summary>
        public static GameManager Instance
        {
            get
            {
                if (m_Instance == null)
                {
                    m_Instance = new GameManager();
                }
                return m_Instance;
            }
        }

        #endregion

        /// <summary>
        /// Removes a match from the list of games that a particular server is tracking - GamesPerServerMap
        /// </summary>
        /// <param name="serverName">the server to remove this game from</param>
        /// <param name="id">the id of the game to remove</param>
        /// <param name="lockData">if the match data should be thread-synchronized.  set yes, if you know this method is called first in the stack (i.e. when you're calling it as a response to an inbound packet). Using the wrong will value will decrease performance as unnecessary thread Monitors will be set</param>
        public void RemoveGameFromServerTracking(Guid id, bool lockData)
        {
            if (lockData)
            {
                Monitor.Enter(MatchDataLock);
            }

            try
            {
                DB.Instance.Lobby_UntrackGameForServer(id);
                GameServerGame sg = null;
                if (Games.TryGetValue(id, out sg))
                {
                    if (Games.Remove(id))
                    {
                        Log1.Logger("Server").Info("Removed game [" + id.ToString() + "] from server. Now tracking [" + Games.Count.ToString() +" games] on this server.");
                    }

                    m_TotalProjectedPlayerCount -= sg.MaxObservers + sg.MaxPlayers;
                    Log1.Logger("Server").Debug("Padded connection count for server is now [" + m_TotalProjectedPlayerCount + "].");
                }
            }
            catch { }
            finally
            {
                if (lockData)
                {
                    Monitor.Exit(MatchDataLock);
                }
            }
        }

        /// <summary>
        /// Adds a match to the GamesPerServerMap list of games that a particular server is tracking
        /// </summary>
        /// <param name="serverName">the name of the server to register this game under</param>
        /// <param name="game">the game to register</param>
        /// <param name="lockData">if the match data should be thread-synchronized.  set yes, if you know this method is called first in the stack (i.e. when you're calling it as a response to an inbound packet). Using the wrong will value will decrease performance as unnecessary thread Monitors will be set</param>
        public bool CreateNewGame(GameServerGame game, bool lockData)
        {
            if (lockData)
            {
                Monitor.Enter(MatchDataLock);
            }

            try
            {
                if (GameCreationRequest != null)
                {
                    if (!GameCreationRequest(game))
                    {
                        return false;
                    }
                }
                if (Games.ContainsKey(game.GameID))
                {
                    // hm. we already know about that game.  probably not great.
#if DEBUG
                    throw new ArgumentException("Tried AddGameToServerTracking for a game that we alrady knew about.");   
#endif
                    return false;
                }
                
                Games.Add(game.GameID, game);
                m_TotalProjectedPlayerCount += (game.MaxPlayers + game.MaxObservers);
                Log1.Logger("Server").Debug("Padded connection count for server is now [" + m_TotalProjectedPlayerCount + "].");
                Log1.Logger("Server").Info("After adding this one, we are tracking [" + Games.Count.ToString() + " games] on this server now.");

                if(GameCreated!= null)
                {
                    GameCreated(game);
                }
            }
            catch 
            {
                return false;
            }
            finally
            {
                if (lockData)
                {
                    Monitor.Exit(MatchDataLock);
                }
            }
            
            return true;
        }

        /// <summary>
        /// Gets the game, if this server knows about, regardless if it's local or not
        /// </summary>
        /// <param name="game">the id of the game we want to fetch</param>
        /// <param name="sg">the game, if we found it</param>
        /// <returns>true if we know about the game, false otherwise</returns>
        public bool GetGame(Guid game, out GameServerGame sg)
        {
            return Games.TryGetValue(game, out sg);
        }


        #region Messaging

        public void SendMatchChangeNotificationToPlayers(MatchNotificationType kind, GameServerGame theGame, ServerCharacterInfo targetPlayer)
        {
            string text = "";
            switch (kind)
            {
                case MatchNotificationType.PlayerRemoved:
                    text = "Goodbye, " + targetPlayer.CharacterName;
                    break;
                case MatchNotificationType.PlayerAdded:
                    text = "Welcome, " + targetPlayer.CharacterName;
                    break;
                case MatchNotificationType.MatchEnded:
                    text = "Game '" + theGame.Name + "' ended.";
                    break;
            }

            theGame.SendMatchChangeNotificationToPlayers(kind, targetPlayer, text, false);
        }


        #endregion
    }
}
