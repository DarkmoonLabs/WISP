using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;

namespace Shared
{
    /// <summary>
    /// Caches character information on the game server.  A character object may be cached if it has been 
    /// activated by its owner, or if it has been loaded from the DB for some other reason.
    /// Activated characters do not expire from the cache until the owning user has abandoned them (i.e. has either
    /// activated another character or has logged out).  All other characters will expire from the cache after a certain
    /// amount of time.
    /// </summary>
    public class CharacterCache
    {
        static CharacterCache()
        {
            m_TimeoutTimer = new Timer();
            m_TimeoutTimer.Interval = 30 * 1000; // 30 seconds
            m_TimeoutTimer.Elapsed += new ElapsedEventHandler(OnTimeoutTimerElapsed);
            m_TimeoutTimer.Start();
        }

        static void OnTimeoutTimerElapsed(object sender, ElapsedEventArgs e)
        {
            m_TimeoutTimer.Stop();
            // characters about that have expired
            List<ServerCharacterInfo> expired = new List<ServerCharacterInfo>();
            
            lock (m_CharacterMap)
            {
                Dictionary<int, CacheItem>.Enumerator enu = m_CharacterMap.GetEnumerator();
                DateTime aboutTo = DateTime.UtcNow + TimeSpan.FromSeconds(30);
                DateTime now = DateTime.UtcNow;
                while (enu.MoveNext())
                {
                    if (enu.Current.Value.ExpirationTime < now)
                    {
                        expired.Add(enu.Current.Value.Character);
                    }
                }

                // remove expired
                for (int i = 0; i < expired.Count; i++)
                {
                    Log1.Logger("Server.Character").Debug("Character cache expired for character " + expired[i].CharacterInfo.CharacterName + " owned by " + expired[i].Owner.ToString());
                    m_CharacterMap.Remove(expired[i].CharacterInfo.ID);
                }
            }

            // notify host of pending expiration
            m_TimeoutTimer.Start();
        }

        private class CacheItem
        {
            /// <summary>
            /// The server that is currently hosting the character
            /// </summary>
            public string Host = "";
            public ServerCharacterInfo Character = null;
            public DateTime ExpirationTime = DateTime.MaxValue;
        }

        /// <summary>
        /// used to expire cached toons
        /// </summary>
        private static Timer m_TimeoutTimer;

        private static TimeSpan? m_ExpirationInterval;
        /// <summary>
        /// How long it takes for a character to expire from the cache after it has been adandoned or 
        /// ad-hoc loaded from the DB.  Can be set with App.Config option "CharacterCacheExpirationIntervalMins".
        /// The default is 5 minutes.  The minimum allowed value is 1 minute.  Central server keeps a list of all
        /// characters on all content servers (to facilitate cross-server messaging).  
        /// </summary>
        public static TimeSpan ExpirationInterval
        {
            get 
            {
                if (m_ExpirationInterval == null)
                {
                    ExpirationInterval = TimeSpan.FromMinutes(ConfigHelper.GetIntConfig("CharacterCacheExpirationIntervalMins", 5));
                }

                return m_ExpirationInterval.GetValueOrDefault(TimeSpan.FromMinutes(5));
            }
            set 
            {
                if (value < TimeSpan.FromMinutes(1))
                {
                    m_ExpirationInterval = TimeSpan.FromMinutes(1);
                }
                else
                {
                    m_ExpirationInterval = value;
                }
            }
        }

        /// <summary>
        /// stores all currently known characters.
        /// </summary>
        private static Dictionary<int, CacheItem> m_CharacterMap = new Dictionary<int, CacheItem>();

        /// <summary>
        /// Retrieves a character from the cache.  If that character does not currently exist in the cache, it can optionally 
        /// be loaded from the DB.  
        /// </summary>
        /// <param name="characterId">the ID of the character to retrieve</param>
        /// <param name="searchDb"></param>
        /// <returns></returns>
        public static ServerCharacterInfo GetCharacter(int characterId)
        {
            CacheItem item = null;
            lock (m_CharacterMap)
            {
                m_CharacterMap.TryGetValue(characterId, out item);
            }

            if (item != null)
            {
                return item.Character;
            }

            return null;
        }

        /// <summary>
        /// Add a character to the cache, or renew that character's expiration timer if it's already in the cache
        /// </summary>
        /// <param name="character">the character to cache</param>
        /// <param name="hostServer">the server hosting the character</param>
        public static void CacheCharacter(ServerCharacterInfo character, string hostServer)
        {
            CacheCharacter(character, hostServer, ExpirationInterval);
        }

        /// <summary>
        /// If the character with the given ID is currently connected to a server in the cluster, this method
        /// returns that server's ServerUserID.
        /// </summary>
        /// <param name="characterId">the id of the character to look up</param>
        /// <returns></returns>
        public static string GetCurrentCharacterHost(int characterId)
        {
            CacheItem item = null;
            lock (m_CharacterMap)
            {
                m_CharacterMap.TryGetValue(characterId, out item);
            }

            if (item != null)
            {
                return item.Host;
            }

            return "";
        }

        /// <summary>
        /// Checks the cache for the indicated character and, if it exists, reads that character's connection to see
        /// if it's alive and ok to send on
        /// </summary>
        /// <param name="characterId">the character to check against</param>
        /// <returns></returns>
        public static bool IsCharacterConnectionAlive(int characterId)
        {
            CacheItem item = null;
            lock (m_CharacterMap)
            {
                m_CharacterMap.TryGetValue(characterId, out item);
                if (item != null && item.Character.OwningAccount != null && item.Character.OwningAccount.MyConnection != null)
                {
                    return item.Character.OwningAccount.MyConnection.IsAlive;
                }
            }
            return false;
        }

        /// <summary>
        /// Every character currently in the cache
        /// </summary>
        public static List<ServerCharacterInfo> AllCharacters
        {
            get
            {
                List<ServerCharacterInfo> toons = new List<ServerCharacterInfo>();
                lock (m_CharacterMap)
                {
                    Dictionary<int, CacheItem>.Enumerator enu = m_CharacterMap.GetEnumerator();
                    while (enu.MoveNext())
                    {
                        toons.Add(enu.Current.Value.Character);
                    }
                }

                return toons;
            }
        }

        /// <summary>
        /// Updates the cache expiration time for a given character
        /// </summary>
        /// <param name="characterId">the character to update</param>
        /// <param name="cacheDuration">how much time before the character expires from the cache</param>
        /// <returns></returns>
        public static bool UpdateCacheTime(int characterId, TimeSpan cacheDuration)
        {
            CacheItem item = null;

            lock (m_CharacterMap)
            {
                if (!m_CharacterMap.TryGetValue(characterId, out item))
                {
                    return false;
                }

                if (cacheDuration == TimeSpan.MaxValue)
                {
                    item.ExpirationTime = DateTime.MaxValue;
                }
                else
                {
                    try
                    {
                        item.ExpirationTime = DateTime.UtcNow + cacheDuration;
                    }
                    catch (ArgumentOutOfRangeException exc)
                    {
                        item.ExpirationTime = DateTime.MaxValue;
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// Add a character to the cache, or renew that character's expiration timer if it's already in the cache
        /// </summary>
        /// <param name="character">the character to cache</param>
        /// <param name="hostServer">the server hosting the character</param>
        /// <param name="cacheDuration">The amount of time before the character expires from the cache</param>
        public static void CacheCharacter(ServerCharacterInfo character, string hostServer, TimeSpan cacheDuration)
        {
            if (character == null)
            {
                return;
            }

            CacheItem item = null;

            lock (m_CharacterMap)
            {
                Log1.Logger("Server.Character").Debug("Caching character [" + character.CharacterName + "|#" + character.ID + "]");
                if (!m_CharacterMap.TryGetValue(character.CharacterInfo.ID, out item))
                {
                    item = new CacheItem();
                    m_CharacterMap.Add(character.CharacterInfo.ID, item);
                }

                item.Host = hostServer;
                item.Character = character;
                if (cacheDuration == TimeSpan.MaxValue)
                {
                    item.ExpirationTime = DateTime.MaxValue;
                }
                else
                {
                    try
                    {
                        item.ExpirationTime = DateTime.UtcNow + cacheDuration;
                    }
                    catch (ArgumentOutOfRangeException exc)
                    {
                        item.ExpirationTime = DateTime.MaxValue;
                    }
                }
            }
        }

        /// <summary>
        /// Immediately remove a character from the cache
        /// </summary>
        /// <param name="characterId">the character to remove</param>
        public static void UncacheCharacter(int characterId)
        {
            lock (m_CharacterMap)
            {
                if (m_CharacterMap.Remove(characterId))
                {
                    Log1.Logger("Server.Character").Debug("Uncached character [#" + characterId + "]");
                }
            }
        }        
    }
}
