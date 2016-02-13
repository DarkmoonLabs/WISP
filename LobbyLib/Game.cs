using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Shared;

namespace Shared
{
    /// <summary>
    /// Used for formatting display text within the game objcts
    /// </summary>
    public enum GameStringType
    {
        ChatText,
        PlayerName,
        SystemMessage,
        PrivateMessage
    }

    /// <summary>
    /// Encapsulates a match/game/room/instance
    /// </summary>
    public class Game : Component, IGame
    {        
        private static uint m_TypeHash = 0;
        public override uint TypeHash
        {
            get
            {
                if (m_TypeHash == 0)
                {
                    m_TypeHash = Factory.GetTypeHash(this.GetType());
                }

                return m_TypeHash;
            }
        }

        /// <summary>
        /// WISP Game objects in the game.
        /// </summary>
        public GameObjectManager ObjectManager
        {
            get;
            set;
        }

        /// <summary>
        /// All the character IDs who have quit the game after it was kicked off
        /// </summary>
        public int[] Quitters
        {
            get
            {
                return Properties.GetIntArrayProperty("Quitters");
            }
        }
        
        /// <summary>
        /// Informative message log (chat, system messages, etc)
        /// </summary>
        public FramedList<string> Chat = new FramedList<string>();

        /// <summary>
        /// Chat log as a single string
        /// </summary>
        public string ChatString = "";

        /// <summary>
        /// Max number of messages (chat, info, etc) to keep.
        /// </summary>
        public int MaxMessages = 100;

        public bool AllPlayerClientsLoaded()
        {
            lock (AllPlayersSyncRoot)
            {
                foreach (ICharacterInfo ci in AllPlayers)
                {
                    if (Properties.GetSinglelProperty(ci.ID + "_clientloadpercent").GetValueOrDefault(0) < 100f)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public bool WaitingOnClientsToLoad
        {
            get
            {
                return Properties.GetBoolProperty("WaitingOnClientsToLoad").GetValueOrDefault(true);
            }
            set
            {
                Properties.SetProperty("WaitingOnClientsToLoad", value);
            }
        }

        public Game()
        {
            GameID = Guid.NewGuid();            
            Players = new Dictionary<int, ICharacterInfo>();
            Observers = new Dictionary<int, ICharacterInfo>();
            EverActivePlayers = new Dictionary<int, ICharacterInfo>();
            IsShuttingDown = false;
            Properties = new PropertyBag("GameProperties");
            Properties.SubscribeToChangeNotifications(this);
            Owner = 0;
            AbandonedTimestamp = DateTime.MaxValue;
            ObjectManager = new GameObjectManager(GameID);
        }

        public Game(IGame decorator) : this()
        {
            Decorator = decorator;
        }

        /// <summary>
        /// Once a game is out of the lobby, it has started.  Even when a game is shutting down, it still "Started" some time ago. 
        /// In other words, as long as this game has ever made it out of the lobby phase, this property will be true.
        /// </summary>
        public bool Started 
        {
            get
            {
                return Properties.GetBoolProperty("Started").GetValueOrDefault(false);
            }
            set
            {
                Properties.SetProperty("Started", value);
            }
        }

        /// <summary>
        /// Once the match/game/round/instance has concluded, i.e. the content was consumed, this property will be true.  Note that game
        /// may Ended == True while BeenSolved == False.  This would occur if all players abandoned the game while in progress.
        /// </summary>
        public bool Solved 
        {
            get
            {
                return Properties.GetBoolProperty("Solved").GetValueOrDefault(false);
            }
            set
            {
                Properties.SetProperty("Solved", value);
            }
        }

        /// <summary>
        /// True if the game either Solved or Abandoned
        /// </summary>
        public bool Ended 
        {
            get
            {
                return Properties.GetBoolProperty("Ended").GetValueOrDefault(false);
            }
            set
            {
                Properties.SetProperty("Ended", value);
            }
        }

        /// <summary>
        /// Unique ID for this game instance
        /// </summary>
        public Guid GameID { get; set; }
        
        /// <summary>
        /// Player who owns this game, by default the person who created it
        /// </summary>
        public int Owner 
        { 
            get
            {
                return Properties.GetIntProperty("Owner").GetValueOrDefault(-1);
            }
            set
            {
                Properties.SetProperty("Owner", value);
            }
        }

        /// <summary>
        /// Checks to see if the player is part of this game, currently
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool IsPlayerPartOfGame(int id)
        {
            return Players.ContainsKey(id);
        }

        /// <summary>
        /// Checks to see if that player currently an observer
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool IsPlayerObserver(int id)
        {
            return Observers.ContainsKey(id);
        }

        /// <summary>
        /// Active participants in the game
        /// </summary>
        public Dictionary<int, ICharacterInfo> Players
        {
            get 
            {
                return m_Players;
            }
            set { m_Players = value; }
        }
        private Dictionary<int, ICharacterInfo> m_Players;        

        /// <summary>
        /// People getting updates about the game, but not actively participating.
        /// </summary>
        public Dictionary<int, ICharacterInfo> Observers { get; set; }

        private List<ICharacterInfo> m_AllPlayers = null;
        private object m_AllPlayersSyncRoot = new object();
        public object AllPlayersSyncRoot
        {
            get
            {
                return m_AllPlayersSyncRoot;
            }
        }

        /// <summary>
        /// These are all of the players that were ever added to the game AFTER the game was out of the lobby...
        /// i.e. all the players that ever participated in the game, wether or not they are still connected.
        /// Use AllPlayers or Players if you want only currently attached players.
        /// </summary>
        public Dictionary<int, ICharacterInfo> EverActivePlayers { get; set; }

        /// <summary>
        /// Gets a player from the character list, or null if that player isnt part of the game.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ICharacterInfo GetPlayer(int id)
        {
            return GetPlayer(id, true);
        }

        /// <summary>
        /// Gets a player from the character list, or null if that player isnt part of the game.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ICharacterInfo GetPlayer(int id, bool onlyCurrentlyAttached)
        {
            ICharacterInfo ci = null;
            Players.TryGetValue(id, out ci);
            if(ci == null && !onlyCurrentlyAttached)
            {
                EverActivePlayers.TryGetValue(id, out ci);
            }
            return ci as ICharacterInfo;
        }

        /// <summary>
        /// Convenience property to access a list of all characters in the Players dictionary
        /// </summary>
        public List<ICharacterInfo> AllPlayers
        {
            get
            {
                lock (AllPlayersSyncRoot)
                {
                    if (m_AllPlayers == null)
                    {
                        m_AllPlayers = (List<ICharacterInfo>)Players.Values.ToList<ICharacterInfo>();
                    }
                    return m_AllPlayers;
                }
            }
            set
            {
                m_AllPlayers = value;
            }
        }

        /// <summary>
        /// Arbitrary property bag for game specific data
        /// </summary>
        public PropertyBag Properties { get; set; }

        /// <summary>
        /// When a match is killed, we ask to D/C all players.  once all player's are D/Cd, 
        /// we actually remove the match from the server.
        /// this variable tracks wether or not that asynchrounous, multi-step process has been begun
        /// </summary>
        public bool IsShuttingDown { get; set; }

        /// <summary>
        /// If all players leave a game, the game is abandoned.  this time stamp is used to determine
        /// when to purge the game.  abandoned games that are in progress have a limited life span
        /// </summary>
        public DateTime AbandonedTimestamp { get; set; }

        /// <summary>
        /// Convenience accessor to grab the value of Properties["maxplayers"]. 
        /// The maximum number of players that can participate in this game.
        /// </summary>
        public int MaxPlayers
        { 
            get
            {
                int? val = Properties.GetIntProperty((int)PropertyID.MaxPlayers);
                return val.GetValueOrDefault(-1);
            }
            set
            {
                Properties.SetProperty("maxplayers", (int)PropertyID.MaxPlayers, value);
            }
        }

        /// <summary>
        /// Convenience accessor to grab the value of Properties["maxobservers"]. 
        /// The maximum number of observers that can watch a game.
        /// </summary>
        public int MaxObservers
        {
            get
            {
                int? val = Properties.GetIntProperty((int)PropertyID.MaxObservers);
                return val.GetValueOrDefault(-1);
            }
            set
            {
                Properties.SetProperty("maxobservers", (int)PropertyID.MaxObservers, value);
            }
        }

        /// <summary>
        /// Convenience accessor to grab the value of Properties["name"]. Player facing name or title for this instance.
        /// </summary>
        public string ComponentName
        {
            get
            {
                string val = Properties.GetStringProperty((int)PropertyID.Name);
                return val;
            }
            set
            {
                Properties.SetProperty((int)PropertyID.Name, value);
            }
        }

        /// <summary>
        /// UI friendly name of the game.
        /// </summary>
        public string Name 
        {
            get
            {
                return ComponentName;
            }
            set
            {
                ComponentName = value;
            }
        }

        /// <summary>
        /// Add a piece of text to the message store for this game.  Usually just used for display purposes.  Includes Chat, system messages, etc.
        /// This method only adds the message to the LOCAL copy of this log.
        /// </summary>
        /// <param name="msg"></param>
        public void AddMessage(string msg)
        {
            if (msg == null || msg.Length < 1)
            {
                return;
            }

            lock (Chat)
            {
                Chat.Add(msg);
                bool removed = false;
                while (Chat.Count > MaxMessages)
                {
                    removed = true;
                    Chat.RemoveAt(0);
                }

                if (removed)
                {
                    RebuildChatString();
                }
                else
                {
                    ChatString += msg + Environment.NewLine;
                }
            }
        }       

        protected virtual string FormatString(GameStringType t, string text)
        {
            return text;
        }

        /// <summary>
        /// Rebuilds the chat string using the contents of the Chat log.  Normally, this does not need to be called except when the Chat log is modified directly and you want
        /// the changes to be reflected in the UI
        /// </summary>
        public void RebuildChatString()
        {
            for (int i = 0; i < Chat.Count; i++)
            {
                ChatString += Chat[i] + Environment.NewLine;
            }
        }

        /// <summary>
        /// If this object is being decorated by another Game object, this will be a reference to it.
        /// </summary>
        public IGame Decorator = null;
        
        /// <summary>
        /// Fires when a property in the property bag has changed
        /// </summary>
        public virtual void OnPropertyUpdated(Guid bag, Property p)
        {
            if (Decorator != null)
            {
                Decorator.OnPropertyUpdated(bag, p);
            }
        }

        /// <summary>
        /// Fires when a property in the property bag has been added
        /// </summary>
        public virtual void OnPropertyAdded(Guid bag, Property p)
        {
            if (Decorator != null)
            {
                Decorator.OnPropertyAdded(bag, p);
            }
        }

        /// <summary>
        /// Fires when a property in the property bag has been added
        /// </summary>
        public virtual void OnPropertyRemoved(Guid bag, Property p)
        {
            if (Decorator != null)
            {
                Decorator.OnPropertyRemoved(bag, p);
            }
        }

        public override void Serialize(ref byte[] buffer, Pointer p, bool includeSubComponents)
        {                        
            // General match info
            BitPacker.AddInt(ref buffer, p, Owner);
            BitPacker.AddString(ref buffer, p, GameID.ToString());
            BitPacker.AddPropertyBag(ref buffer, p, Properties);

            // Players
            List<ICharacterInfo> players = AllPlayers;
            BitPacker.AddInt(ref buffer, p, players.Count);
            for (int i = 0; i < players.Count; i++)
            {
                ICharacterInfo ci = players[i];
                BitPacker.AddPropertyBag(ref buffer, p, ci.Properties);
                BitPacker.AddStatBag(ref buffer, p, ci.Stats);
                BitPacker.AddInt(ref buffer, p, ci.ID);
            }

            base.Serialize(ref buffer, p, includeSubComponents);
        }

        public override void Deserialize(byte[] data, Pointer p, bool includeSubComponents)
        {
            // General match info
            Owner = BitPacker.GetInt(data, p);
            GameID = new Guid(BitPacker.GetString(data, p));

            // Options
            Properties = BitPacker.GetPropertyBag(data, p);

            // Players
            int numPlayers = BitPacker.GetInt(data, p);
            for (int i = 0; i < numPlayers; i++)
            {
                CharacterInfo ci = new CharacterInfo();
                ci.Properties = BitPacker.GetPropertyBag(data, p);
                ci.Stats = BitPacker.GetStatBag(data, p);
                ci.ID = BitPacker.GetInt(data, p);
                Players.Add(ci.ID, ci);
            }

            base.Deserialize(data, p, includeSubComponents);
        }

        public object CurrentGameStateSyncRoot = new object();

        protected GameState m_CurrentGameState = GameState.Lobby;
        /// <summary>
        /// Current state of the game
        /// </summary>
        public GameState CurrentGameState
        {
            get
            {
                lock (CurrentGameStateSyncRoot)
                {
                    return m_CurrentGameState;
                }
            }
            set
            {
                m_CurrentGameState = value;
            }
        }

        /// <summary>
        /// Lock this to modofy the AllObservers field
        /// </summary>
        private object m_AllObserversSyncRoot = new object();
        public object AllObserversSyncRoot
        {
            get { return m_AllObserversSyncRoot; }
        }
        
        /// <summary>
        /// Convenience property to access a list of all characters in the Players dictionary
        /// </summary>
        public List<ICharacterInfo> AllObservers
        {
            get
            {
                lock (AllObserversSyncRoot)
                {
                    if (m_AllObservers == null)
                    {
                        m_AllObservers = (List<ICharacterInfo>)Observers.Values.ToList<ICharacterInfo>();
                    }
                    return m_AllObservers;
                }
            }
            set
            {
                lock (AllObserversSyncRoot)
                {
                    m_AllObservers = value;
                }
            }
        }
        private List<ICharacterInfo> m_AllObservers = null;


    }
}
