using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Threading;
using ServerLib;
using System.IO;
using System.Configuration;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;

namespace Shared
{
    /// <summary>
    /// Information about a group of GameServerInfo objects, as configured in App.Config.
    /// </summary>
    public class GameServerInfoGroup
    {
        public GameServerInfoGroup()
        {
            CurConnectionIndex = 0;
            OutboundServers = new List<GameServerInfo<OutboundServerConnection>>();
        }

        /// <summary>
        /// Are any of the group's connections currently live?
        /// </summary>
        public bool HasLiveOutboundServerConnections { get; set; }
        
        /// <summary>
        /// When ConnectMode is round robin, we keep track of which index we're currently connected to
        /// </summary>
        public int CurConnectionIndex { get; set; }
        /// <summary>
        /// Unique ID for the server group
        /// </summary>
        public string ID { get; set; }

        /// <summary>
        /// Default Hive key that all servers in the group will use unless they have a specific one specified
        /// </summary>
        public string SharedHiveKey { get; set; }

        /// <summary>
        /// How we should connect to the servers in the group
        /// </summary>
        public OutgoingServerConnectMethod ConnectMode { get; set; }

        /// <summary>
        /// Servers within this group that we should connect to
        /// </summary>
        public List<GameServerInfo<OutboundServerConnection>> OutboundServers = new List<GameServerInfo<OutboundServerConnection>>();

    
        /// <summary>
        /// Gets the next LIVE connection in the rotation. It's essentially a poor man's load balancer that respects the ConnectMode for the group.
        /// Returns null if all servers in the group are offline or if there are no servers in the group.
        /// </summary>
        /// <returns></returns>
        public GameServerInfo<OutboundServerConnection> NextConnection()
        {
            List<GameServerInfo<OutboundServerConnection>> liveServers = new List<GameServerInfo<OutboundServerConnection>>();
            
            // Only consider online servers
            liveServers = OutboundServers.FindAll(con => con.IsOnline);
            if (liveServers.Count == 0)
            {
                return null;
            }

            if (ConnectMode == OutgoingServerConnectMethod.Random)
            {
                return liveServers[m_Random.Next(0, liveServers.Count)];
            }

            GameServerInfo<OutboundServerConnection> gsi = null;
            if (liveServers.Count == 1)
            {
                gsi = liveServers[0];
            }

            lock (m_SyncRoot)
            {
                m_CurNextConnection++;
                if (m_CurNextConnection >= liveServers.Count)
                {
                    m_CurNextConnection = 0;
                }
                gsi = liveServers[m_CurNextConnection];
            }

            return gsi;
        }

        private static object m_SyncRoot = new object();
        private static Random m_Random = new Random();

        /// <summary>
        /// Used for NextConnection RoundRobin load balancing.
        /// </summary>
        private int m_CurNextConnection = 0;
    }

    /// <summary>
    /// All GameServerInfoGroups that have been defined in the App.Config
    /// </summary>
    public class OutboundServerGroups
    {
        public OutboundServerGroups()
        {
            Groups = new Dictionary<string, GameServerInfoGroup>();
        }

        public Dictionary<string, GameServerInfoGroup> Groups { get; set; }
        
        /// <summary>
        /// The maximum connection count that all servers in the OutgoingConnections will take up, given the ConnectMode of each group
        /// </summary>
        public int TotalConnectionsAllocated 
        {
            get
            {
                Dictionary<string, GameServerInfoGroup>.Enumerator enu = Groups.GetEnumerator();
                int total = 0;
                while (enu.MoveNext())
                {
                    if (enu.Current.Value.ConnectMode == OutgoingServerConnectMethod.All)
                    {
                        total += enu.Current.Value.OutboundServers.Count;
                    }
                    else
                    {
                        total++;
                    }
                }

                return total;
            }
        }

        /// <summary>
        /// Gets the connections for the "Default" server group.
        /// </summary>
        public List<GameServerInfo<OutboundServerConnection>> DefaultGroupConnections 
        {
            get
            {
                return GetGroupConnections("Default");
            }
        }

        /// <summary>
        /// Does the 'Default' server group have any live outgoing connections?
        /// </summary>
        public bool DefaultGroupHasLiveOutboundServerConnections { get; set; }

        /// <summary>
        /// Gets the connections associated with a particular Server Group
        /// </summary>
        /// <param name="groupName"></param>
        /// <returns></returns>
        public List<GameServerInfo<OutboundServerConnection>> GetGroupConnections(string groupName)
        {
            GameServerInfoGroup gsg = null;
            if (!Groups.TryGetValue(groupName, out gsg))
            {
                return new List<GameServerInfo<OutboundServerConnection>>();
            }

            return gsg.OutboundServers;
        }

        public GameServerInfoGroup this[string id]
        {
            get
            {
                GameServerInfoGroup gsg = null;
                if (id == null)
                {
                    return gsg;
                }
                Groups.TryGetValue(id, out gsg);
                return gsg;
            }
        }
    }

    public enum OutgoingServerConnectMethod
    {
        /// <summary>
        /// An attempt will be made to keep a connection with ALL of the listed servers at all times.
        /// </summary>
        All,

        /// <summary>
        /// One server in the list will be connected to at a time, starting with a random choice in the list.  If a connection is closed, the next one in the list is chosen.
        /// </summary>
        RoundRobin,

        /// <summary>
        /// One server in the list will be connected to at a time, starting with a random choice in the list.  If a connection is closed, a different connection in the list will be chosen at random.
        /// </summary>
        Random
    }

	/// <summary>
	/// Basic Server Object.  Listens for connections on a given port
	/// </summary>
	public abstract class ServerBase
	{
		#region Data

        public Dictionary<string, string> AppConfig { get; set; }

        /// <summary>
        /// If and how players must authenticate against the accounts DB in order to log in.  If this is set to false, nothing about the session will be persisted, including characters.
        /// If you set this to False, UseCharacters will also be set to False, regardless of what that setting says in App.Config.
        /// </summary>
        public bool RequireAuthentication = false;

        /// <summary>
        /// A server hive can be configured, via App.Config option "UseCharacters = True/False", to not use characters.  When a 
        /// server doesn't use characters (i.e. personas, identities, etc), a default character will be created when a new account
        /// is created.  That default character will be associated with the user's account and used throughout the API wherever a
        /// reference to a character is required.  If UseCharacters = True, then no default character will be created, and the
        /// API will respond negatively if a player tries to access content on the hive without first creating and activating 
        /// (i.e. choosing) a character to play with.  In practice what this means, is that if you set UseCharacters to true, you
        /// must create (DB.Character_Create;) and set (ServerUser.CurrentCharacter = Foo;) the appropriate characters manually 
        /// (i.e. via some UI element, or programmaticaly in your code) (Login server can handle this for you).  
        /// The default value for this prooperty is False. If RequireAuthentication is set to False, this property will always
        /// be false.
        /// </summary>
        public bool UseCharacters = false;

        /// <summary>
        /// Sockets in blocking mode don't send packet asynchronously.  
        /// Will try to send a packet until either it goes through or the connection dies.
        /// </summary>
        public bool BlockingMode = true;

        /// <summary>
        /// If true, Nagel's algorithm is turned off for the TCP connection, i.e. we set the TCP_NODELAY option on all sockets. 
        /// If you don't know what this does, leave it set to false.  
        /// </summary>
        public bool DisableTCPDelay = false;

        /// <summary>
		/// All servers in a cluster share a secret which is used when they connect to one another.  Shared cluster keys are specified in the App.Config of each server instance.
		/// Servers that share the same cluster secret can log in to each other.
		/// </summary>
		public Guid SharedClusterServerSecret;
		private Socket m_Listener;

        /// <summary>
		/// The port on which the server listens
		/// </summary>
		public int ListenOnPort { get; set; }

		/// <summary>
		/// The name of the server
		/// </summary>
		public string ServerName { get; set; }

        /// <summary>
        /// All of the servers that we are to connect to.
        /// </summary>
        public OutboundServerGroups OutboundServerGroups { get; set; }

		/// <summary>
		/// Maximum number of inbound connections that this server will accept
		/// </summary>
		public int MaxInboundConnections
		{
			get
			{
                return MaxConnections - OutboundServerGroups.TotalConnectionsAllocated;
			}
		}

        /// <summary>
        /// If the server should check login credentials when a player attempts a direct connection with the server.  This might be helpful if you don't
        /// require the player to have an account to connect to the server.  When this property is set to true, a default ServerUser object will be created
        /// for the connecting client.
        /// </summary>
         public static bool SupressLoginVerify { get; set; }

		/// <summary>
		/// Total Maximum connections that this server will handle, including connections that are outbound
		/// </summary>
		public int MaxConnections { get; set; }

		/// <summary>
		/// When a server connects to another server in the cluster, it uses this Username to identify itself.  All servers in the cluster must have
		/// unique ServerUserIDs, otherwise if two servers with the same ServerUserID try to log in to the same peer, they will continually cause each other
		/// to be disconnected.
		/// </summary>
		public string ServerUserID { get; set; }

		/// <summary>
		/// Size of the socket send/receive buffers.  1024 seems like a reasonable starting point.
		/// </summary>
		public Int32 BufferSize = 25;

		/// <summary>
		/// This is the maximum number of asynchronous accept operations that can be 
		///posted simultaneously. This determines the size of the pool of 
		///SocketAsyncEventArgs objects that do accept operations. Note that this
		///is NOT the same as the maximum # of connections.
		/// </summary>
		public Int32 MaxSimultaneousAcceptOps = 4;

		/// <summary>
		/// The size of the queue of incoming connections for the listen socket.
		/// </summary>
		public Int32 MaxConnectionBacklog = 100;

		/// <summary>
		/// The local IPEndPoint
		/// </summary>
		public IPEndPoint LocalEndPoint;

		/// <summary>
		/// Silverlight policy server.  Must run exactly one instance of a PolicyServer on every
		/// machine that a Silverlight client might try to connect to.  The policy server is turned on and off
		/// in the App.Config with "RunSilverlightPolicyServer" - the default is false.
		/// </summary>
		public PolicyServer PolicyServer;

        /// <summary>
        /// Unity3d Web player network policy server. Required for Unity3d Web clients.  Turn it on and off in the App.Config file with
        /// "RunUnityWebPolicyServer".  The default is false.
        /// </summary>
        public UnityWebPolicyServer UnityPolicyServer;
		
		/// <summary>
		/// Heartbeat timer.  Every server we connect to in OutboundServers is pinged every once in a while for health information
		/// </summary>
		public System.Timers.Timer OutboundServerHeartbeatTimer = new System.Timers.Timer();

        /// <summary>
        /// Listens to all UDP traffic for our listen-on port.
        /// </summary>
        protected UDPListener m_UDPListener;

		#endregion             

		#region Initialization

        /// <summary>
        /// Writes all of the config settings in AppConfig to the AppConfig file.
        /// </summary>
        /// <param name="reload">should the app config file be reloaded adter setting the new values?</param>
        /// <param name="msg">failure message, if any</param>
        /// <returns></returns>
        public bool SaveConfigs(Dictionary<string,string> configs, bool deleteUnset, bool reload, ref string msg)
        {
            msg = "";
            try
            {
                System.Configuration.Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                if (deleteUnset)
                {
                    config.AppSettings.Settings.Clear();
                }

                Dictionary<string,string>.Enumerator enu = configs.GetEnumerator();
                while (enu.MoveNext())
                {
                    string key = enu.Current.Key;
                    string value = enu.Current.Value;

                    if (!Array.Exists<string>(config.AppSettings.Settings.AllKeys, delegate(string s) { return (s == key); }))
                    {
                        Log1.Logger("Server.Commands").Info("Adding new config setting [" + key + " = " + value + "].");
                        config.AppSettings.Settings.Add(key, value);
                    }
                    else
                    {
                        config.AppSettings.Settings[key].Value = value;
                        Log1.Logger("Server.Commands").Info("Updating config value [" + key + " = " + value + "].");
                    }

                }
                
                // Save the configuration file.
                config.Save(ConfigurationSaveMode.Modified);

                // Force a reload of a changed section.
                if(reload) ConfigurationManager.RefreshSection("appSettings");

                return true;
            }
            catch (Exception e)
            {
                Log1.Logger("Server").Error("Failed writing config file. " + e.Message, e);
                return false;
            }
        }

        public static bool UseDatabaseConnectivity { get; set; }

        static ServerBase()
        {
            UseDatabaseConnectivity = ConfigHelper.GetStringConfig("DatabaseConnectivity", "TRUE").ToLower() == "true";
        }

        /// <summary>
        /// The number of seconds to wait in between testing our outgoing server connections for Health
        /// </summary>
        public int OutboundServerUpdateInterval { get; set; }

        /// <summary>
        /// Loads the outgoing server connections from the App.Config file.
        /// </summary>
        private void LoadOutgoingConnections()
        {            
            ConnectionConfigSection section = null;
            try
            {
                section = (ConnectionConfigSection)ConfigurationManager.GetSection("OutgoingConnections");
            }
            catch (Exception e)
            {
                Log1.Logger("Server").Fatal("Error loading config file section 'OutgoingConnections'. " + e.Message);
                return;
            }
            if (ConfigHelper.GetStringConfig("DatabaseConnectivity", "TRUE").ToLower() != "false")
            {
                DB.Instance.AddServerGroupSessionConnection("Default", ConfigurationManager.ConnectionStrings["SessionDataConnectionString"].ConnectionString);
            }
            OutboundServerUpdateInterval = section == null? int.MaxValue : section.UpdateIntervalSecs;
            GameServerInfoGroup group = null;
            if(section != null)
            {
                foreach (GroupElement g in section.Groups)
                {
                    group = new GameServerInfoGroup();
                    group.SharedHiveKey = g.SharedHiveKey;
                    group.ID = g.ID;

                    // DB session, if exists
                    if (g.SessionDataConnectionString.Length > 0)
                    {
                        DB.Instance.AddServerGroupSessionConnection(group.ID, g.SessionDataConnectionString);
                    }
                    else
                    {
                        DB.Instance.AddServerGroupSessionConnection(group.ID, ConfigurationManager.ConnectionStrings["SessionDataConnectionString"].ConnectionString);
                    }

                    string cm = g.ConnectMode.ToLower();
                    switch (cm)
                    {
                        case "roundrobin":
                            group.ConnectMode = OutgoingServerConnectMethod.RoundRobin;
                            break;
                        case "random":
                            group.ConnectMode = OutgoingServerConnectMethod.Random;
                            break;
                        default:
                            group.ConnectMode = OutgoingServerConnectMethod.All;
                            break;
                    }


                    foreach (ConnectionElement con in g.ConnectionItems)
                    {
                        try
                        {
                            GameServerInfo<OutboundServerConnection> gsi = new GameServerInfo<OutboundServerConnection>();
                            gsi.HostName = con.Address;
                            gsi.Name = con.ConnectionName;
                            gsi.ServiceID = con.ServiceID;
                            if (con.SharedHiveKey.Length < 1)
                            {
                                gsi.SharedHiveKey = g.SharedHiveKey;
                            }
                            else
                            {
                                gsi.SharedHiveKey = con.SharedHiveKey;
                            }

                            string ip = con.Address;
                            try
                            {
                                IPHostEntry iphe = Dns.GetHostEntry(ip); // this call will delay the server from starting if it doesn't resolve fast enough.

                                bool gotOne = false;
                                foreach (IPAddress addy in iphe.AddressList)
                                {
                                    if (addy.AddressFamily == AddressFamily.InterNetwork)
                                    {
                                        gotOne = true;
                                        ip = addy.ToString();
                                        break;
                                    }
                                }

                                if (!gotOne)
                                {
                                    Log1.Logger("Server.Network").Error("Could not resolve IP address for server " + gsi.Name + " (" + ip + ")");
                                    continue;
                                }
                            }
                            catch (Exception e)
                            {
                                Log1.Logger("Server.Network").Error("Error setting up outbound server connection. " + gsi.Name + " / " + gsi.HostName + " : " + e.Message, e);
                                // try the next address in the config
                                continue;
                            }

                            if (ip.Trim().Length < 1)
                            {
                                // try the next address in the config
                                continue;
                            }

                            gsi.IP = ip;
                            gsi.Port = con.Port;
                            gsi.IsOnline = false;
                            gsi.LastUpdate = DateTime.UtcNow;

                            if (group.OutboundServers.Exists(xcon => xcon.UniqueID == gsi.UniqueID))
                            {
                                Log1.Logger("Server").Error("Not adding outbound server [" + gsi.Name + "] as it's destination endpoing [" + gsi.UniqueID + "] matches an existing connection.");
                                continue;
                            }

                            gsi.ServerGroup = group.ID;
                            group.OutboundServers.Add(gsi);
                        }
                        finally
                        {
                        }
                    }
                }

                if (group != null)
                {
                    if (group.ConnectMode == OutgoingServerConnectMethod.Random || group.ConnectMode == OutgoingServerConnectMethod.RoundRobin)
                    {
                        // these two connect methods always start with a random index in the list.  this helps a little in balancing the load across many instances of servers, in lieu of a proper load balancer.
                        group.CurConnectionIndex = m_Random.Next(0, group.OutboundServers.Count);
                    }

                    OutboundServerGroups.Groups.Remove(group.ID);
                    OutboundServerGroups.Groups.Add(group.ID, group);
                }
            }
        }

        public string GetPublicIP()
        {
            try
            {
                string direction;
                WebRequest request = WebRequest.Create("http://checkip.dyndns.org/");
                WebResponse response = request.GetResponse();
                StreamReader stream = new StreamReader(response.GetResponseStream());
                direction = stream.ReadToEnd();
                stream.Close();
                response.Close();
                //Search for the ip in the html    
                int first = direction.IndexOf("Address: ") + 9;
                int last = direction.LastIndexOf("</body>");
                direction = direction.Substring(first, last - first);
                return direction;
            }
            catch
            {
                return "127.0.0.1";
            }
        }

        public string GetLanIP()
        {
            try
            { 
                // get local IP addresses
                IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());

                // test if any host IP equals to any local IP or to localhost
                foreach (IPAddress hostIP in localIPs)
                {
                    if (hostIP.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return hostIP.ToString();
                    }
                }
            }
            catch { }
            
            return "127.0.0.1";

        }

		/// <summary>
		/// Handles receiving of new connections and creating individual socket connections for incoming requests
		/// </summary>
		public ServerBase()
		{            
            try
            {
                ServerUserID = ConfigHelper.GetStringConfig("ServerUserID");

                if (ConfigHelper.GetStringConfig("DatabaseConnectivity", "TRUE").ToLower() != "false")
                {
                    DB.Instance.Server_Unregister(ServerUserID);
                    DB.Instance.User_ClearSessionsForServer(ServerUserID);
                }

                OutboundServerGroups = new Shared.OutboundServerGroups();

                AppConfig = new Dictionary<string, string>();
                foreach (string key in ConfigurationManager.AppSettings)
                {
                    string value = ConfigurationManager.AppSettings[key];
                    if (AppConfig.ContainsKey(key))
                    {
                        Log1.Logger("Server").Warn("AppConfig section '" + key + "' exists more than once. Only using first instance.");
                    }
                    AppConfig.Add(key, value);
                }

                // Load server commands from App.Config
                try
                {
                    CommandManager.LoadFromConfig();
                    PerfMon.LoadFromConfig();
                }
                catch(Exception ex1)
                {
                    Log1.Logger("Server").Fatal("Failed to initialize server. " + ex1.Message, ex1);
                    Environment.Exit(-777);
                }

                ServerAddress = ConfigHelper.GetStringConfig("ServerAddress", GetLanIP());
                Log1.Logger("SERVER").Info("Setting local server address as [" + ServerAddress + "]");                

                ServerName = ConfigHelper.GetStringConfig("ServerName");
                Log1.Logger("SERVER").Info("Setting local server name as [" + ServerName + "]");
                MaxConnections = ConfigHelper.GetIntConfig("ConnectionLimit", 1000);
                BlockingMode = ConfigHelper.GetStringConfig("BlockingMode").ToLower() == "true";
                DisableTCPDelay = ConfigHelper.GetStringConfig("DisableTCPDelay").ToLower() == "true";
                MaxConnectionBacklog = ConfigHelper.GetIntConfig("MaxConnectionBacklog", 100);
                BufferSize = ConfigHelper.GetIntConfig("NetworkBufferSize", 1024);
                MaxSimultaneousAcceptOps = ConfigHelper.GetIntConfig("MaxSimultaneousNetworkAcceptOps", 5);
                RequireAuthentication = ConfigHelper.GetStringConfig("RequireAuthentication", "true").ToLower() == "true";
                UseCharacters = ConfigHelper.GetStringConfig("UseCharacters", "false").ToLower() == "true";    

                try
                {
                    if (ConfigHelper.GetStringConfig("SupressStatLoad").ToLower() == "false")
                    {
                        Log1.Logger("Server").Info("Loaded " + StatManager.Instance.AllStats.StatCount.ToString() + " stat definitions.");
                    }
                }
                catch (Exception stat)
                {
                    Log1.Logger("Server").Error("Error loading character Stat definition file.", stat);
                }

                try
                {
                    if (ConfigHelper.GetStringConfig("SupressCharacterLoad").ToLower() == "false")
                    {
                        CharacterUtil.Instance.CharacterTemplateFile = ConfigHelper.GetStringConfig("CharacterTemplateFile", "\\Config\\Character.xml");
                    }
                }
                catch (Exception toon)
                {
                    Log1.Logger("Server").Error("Error loading character template file.", toon);
                }

                SocketAsyncEventArgsCache.Init(BufferSize, MaxConnections, MaxSimultaneousAcceptOps);

                Factory.Instance.Register(typeof(CharacterInfo), delegate { return new CharacterInfo(); });
                Factory.Instance.Register(typeof(ServerCharacterInfo), delegate { return new ServerCharacterInfo(); });
                Factory.Instance.Register(typeof(AccountProfile), delegate { return new AccountProfile("-"); });
                Factory.Instance.Register(typeof(PerfHistory), delegate { return new PerfHistory(); });             

                LoadOutgoingConnections();

                // Setup child server heartbeat pings
                StartOutboundServerUpdate();	
            }
            catch (Exception exc)
            {
                Log1.Logger("Server").Fatal("Failed to initialize server. " + exc.Message, exc);
                Environment.Exit(-777);
            }

	
		}
        public void RegisterTypes(Type type)
        {
            // Register all object scripts
            /*
            var listOfDerivedClasses = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(
                t => t.BaseType != null && t.BaseType == type
                //x => x.IsSubclassOf(type)
                )
                .ToList();
           */

            List<Type> listOfDerivedClasses = new List<Type>();
            Type[] types = Assembly.GetCallingAssembly().GetTypes().Where(x => x.IsSubclassOf(type)).ToArray();

            Zeus.Util.GetDerivedFromNonGeneric(types, type, ref listOfDerivedClasses);

            //var listOfDerivedClasses = Assembly.GetExecutingAssembly().GetTypes().Where((t) => type.IsAssignableFrom(t));

            // var listOfDerivedClasses = Assembly.GetExecutingAssembly().GetTypes().Where(t => type.IsAssignableFrom(t.DeclaringType) && type != t.DeclaringType);

            foreach (Type derived in listOfDerivedClasses)
            {
                ConstructorInfo ci = derived.GetConstructor(Type.EmptyTypes);
                Func<object> create = Zeus.Util.CreateDelegate(ci, typeof(Func<object>)) as Func<object>;
                Factory.Instance.Register(derived, create);
            }
        }

        private bool m_OutboundServerHeartbeatRunning = false;        
        private Random m_Random = new Random();

        /// <summary>
        /// Starts the Outbound server heartbeat update, if it's not already started.  Useful if you add new servers to the OutboundServers collection after startup.
        /// </summary>
        protected void StartOutboundServerUpdate()
        {
            if (m_OutboundServerHeartbeatRunning)
            {
                return;
            }
            
            if (OutboundServerGroups.TotalConnectionsAllocated > 0)
            {
                m_OutboundServerHeartbeatRunning = true;
                int interval = OutboundServerUpdateInterval * 1000;
                if (interval > 20000)
                {
                    // this update replaces the UDP Poke from the connection object. if it takes too long between pokes, the NAT hole will close down.
                    interval = 20000;
                }
                OutboundServerHeartbeatTimer.Interval = interval;
                OutboundServerHeartbeatTimer.Elapsed += new System.Timers.ElapsedEventHandler(OutboundServerHeartbeatTimer_Elapsed);
                OutboundServerHeartbeatTimer_Elapsed(null, null); // manually run it instantly to get an initial reading.  timer will handle calling it after the initial run.           
            }		
        }

		#endregion

		
		#region Outbound Server Handling

		/// <summary>
		/// Gets the GameServerInfo object, given the name
		/// </summary>
		/// <returns></returns>
		public GameServerInfo<OutboundServerConnection> GetOutboundServerByName(string name)
		{
            foreach (GameServerInfoGroup g in OutboundServerGroups.Groups.Values)
            {
                GameServerInfo<OutboundServerConnection> gsi = g.OutboundServers.Find(con => con.IsOnline && con.Name == name);
                if (gsi != null)
                {
                    return gsi;
                }
            }

            return null;
		}

        /// <summary>
        /// Gets the GameServerInfo object, given the account ID of that server
        /// </summary>
        /// <returns></returns>
        public GameServerInfo<OutboundServerConnection> GetOutboundServerByServerUserID(string accountID)
        {
            foreach (GameServerInfoGroup g in OutboundServerGroups.Groups.Values)
            {
                GameServerInfo<OutboundServerConnection> gsi = g.OutboundServers.Find(con => con.IsOnline && con.UserID == accountID);
                if (gsi != null)
                {
                    return gsi;
                }
            }
            return null;
        }

		/// <summary>
		/// Gets the GameServerInfo for the sub-server with the least amount of connections within the 'Default' server group.
		/// </summary>
		/// <returns></returns>
		public GameServerInfo<OutboundServerConnection> GetLowestOutboundServer()
		{
			GameServerInfo<OutboundServerConnection> gsi = null;
			foreach (GameServerInfo<OutboundServerConnection> con in OutboundServerGroups.DefaultGroupConnections)
			{
				if (gsi == null || con.CurUsers < gsi.CurUsers)
				{
					gsi = con;
				}
			}

			return gsi;
		}

		/// <summary>
		/// Updates the HasLiveOutboundServerConnections property with current data
		/// </summary>
		public void UpdateOutboundServerAvailability(string group)
		{
            GameServerInfoGroup g = OutboundServerGroups[group];
            if (g == null)
            {
                return;
            }
            
            foreach(GameServerInfo<OutboundServerConnection> con in g.OutboundServers)
			{
				if (con.IsOnline)
				{
					g.HasLiveOutboundServerConnections = true;
					return;
				}
			}
			g.HasLiveOutboundServerConnections = false;
		}

        /// <summary>
		/// Broadcasts a packet to all attached OutboundServer connections in all server groups.
		/// </summary>
		/// <param name="p">packet to broadcast</param>
		/// <param name="exception">the name of any server to exclude in the broadcast - use this to broadcast to all child servers except the one it originated on</param>
        public void BroadCastToOutboundServers(Packet p, string exception)
        {
            BroadCastToOutboundServers(p, exception, "|||");
        }

		/// <summary>
		/// Broadcasts a packet to all attached OutboundServer connections.
		/// </summary>
		/// <param name="p">packet to broadcast</param>
		/// <param name="exception">the name of any server to exclude in the broadcast - use this to broadcast to all child servers except the one it originated on</param>
		public void BroadCastToOutboundServers(Packet p, string exception, string group)
		{
            Log1.Logger("Server.Network").Debug("Broadcasting " + p.PacketTypeID.ToString() + " to connected Hive servers.");
			byte[] rawPacket = null;
            
            foreach (GameServerInfoGroup g in OutboundServerGroups.Groups.Values)
            {
                if (group != "|||" && g.ID != group)
                {
                    continue;
                }

                foreach (GameServerInfo<OutboundServerConnection> con in g.OutboundServers)
                {
                    if (exception == con.Name)
                    {
                        continue;
                    }

                    if (con != null && con.Connection != null && con.Connection.IsConnected)
                    {
                        if (p.IsEncrypted || rawPacket == null)
                        {
                            // no need to re-serialize the packet for different connections if it's not encrypted
                            rawPacket = con.Connection.SerializePacket(p);
                        }
                        Log1.Logger("Server.Network").Debug("Sending " + p.PacketTypeID.ToString() + " to " + con.Name);
                        con.Connection.Send(rawPacket, p.Flags);
                    }
                }
            }
		}


		/// <summary>
		/// Get a GSI object, given the IP and port of the game server you want to get info about
		/// </summary>
		public GameServerInfo<OutboundServerConnection> GetGSIByIPAndPort(string IP, int port)
		{
            foreach (GameServerInfoGroup g in OutboundServerGroups.Groups.Values)
            {
                GameServerInfo<OutboundServerConnection> gsi = g.OutboundServers.Find(con => con.IP == IP && con.Port == port);
                if (gsi != null)
                {
                    return gsi;
                }
            }
            return null;
		}

		/// <summary>
		/// Update GSI object with resolved IP address
		/// </summary>
		public bool UpdateGSIIP(string oldIP, int port, string newIP, string serverName)
		{
			GameServerInfo<OutboundServerConnection> gsi = null;
            foreach (GameServerInfoGroup g in OutboundServerGroups.Groups.Values)
            {
                gsi = g.OutboundServers.Find(con => con.UniqueID == GameServerInfo<OutboundServerConnection>.GetUniqueID(oldIP, port));
                if (gsi != null)
                {
                    break;
                }
            }

			if (gsi == null)
			{
				return false;
			}

			gsi.IP = newIP;
			gsi.Name = serverName;
			return true;
		}

		/// <summary>
		/// Fires at certain intervals and causes the UpdateGameServerInfo method to be called.
		/// </summary>
		void OutboundServerHeartbeatTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			OutboundServerHeartbeatTimer.Stop();
			UpdateGameServerInfo();
			OutboundServerHeartbeatTimer.Start();
		}     

		/// <summary>
		/// Send heartbeat pings to all our sub-servers, update sub-server online statuses based on when the last ping was returned, recycle sub-server connections when 
		/// we don't get back a ping in a reasonable amount of time
		/// </summary>
        private void UpdateGameServerInfo()
        {
            foreach (GameServerInfoGroup g in OutboundServerGroups.Groups.Values)
            {
                GameServerInfo<OutboundServerConnection> gsi = null;
                bool selectedNext = false;

                for (int i = 0; i < g.OutboundServers.Count; i++)
                {
                    gsi = g.OutboundServers[i];
                    if (g.ConnectMode == OutgoingServerConnectMethod.Random || g.ConnectMode == OutgoingServerConnectMethod.RoundRobin) // we should only connect to one server at a time
                    {
                        // ignore all but the current index
                        if (g.CurConnectionIndex != i)
                        {
                            continue;
                        }
                    }

                    // do we need to recycle the connection?
                    if (gsi.Connection == null || gsi.Connection.Disonnected || (!gsi.Connection.IsConnected && !gsi.Connection.ConnectionInProgress))
                    {
                        gsi.IsOnline = false;
                        UpdateOutboundServerAvailability(g.ID);
                        if (gsi.Connection != null)
                        {
                            gsi.Connection.KillConnection("Recycling connection.");
                            gsi.Connection.SocketKilled -= new SocketKilledDelegate(OutboundConnection_SocketKilled);
                            gsi.Connection.SocketConnectionConcluded -= new Action<IClientConnection, bool, string>(OutboundConnection_SocketConnectionConcluded);
                            gsi.Connection = null;
                        }

                        if (!selectedNext && g.OutboundServers.Count > 1 && g.ConnectMode == OutgoingServerConnectMethod.RoundRobin)
                        {
                            selectedNext = true;
                            g.CurConnectionIndex++;
                            if (g.CurConnectionIndex >= g.OutboundServers.Count)
                            {
                                g.CurConnectionIndex = 0;
                                i = -1;
                            }
                            continue;
                        }

                        if (!selectedNext && g.OutboundServers.Count > 1 && g.ConnectMode== OutgoingServerConnectMethod.Random)
                        {
                            selectedNext = true;
                            // we want to avoid randomly selecting the same index that we're currently on.
                            // try rolling the dice once to see if we hit an index that's not the current one so we can avoid copying the list.  
                            // if we get unlucky and hit the current index with the die roll, then just do it the long way instead of looping the random until we hit a good index
                            int rand = m_Random.Next(0, g.OutboundServers.Count);
                            if (rand == g.CurConnectionIndex) // oh well, nice try
                            {
                                List<GameServerInfo<OutboundServerConnection>> minusOne = new List<GameServerInfo<OutboundServerConnection>>(g.OutboundServers);
                                minusOne.Remove(gsi);
                                int tmp = m_Random.Next(0, minusOne.Count);
                                g.CurConnectionIndex = g.OutboundServers.IndexOf(minusOne[tmp]);
                            }
                            else
                            {
                                // yay!
                                g.CurConnectionIndex = rand;
                            }

                            i = -1;
                            continue;
                        }

                        selectedNext = false;
                        gsi.Connection = CreateOutboundServerConnection(gsi.Name, this, gsi.IP, gsi.ServiceID, BlockingMode);
                        gsi.Connection.BlockingMode = BlockingMode;
                        gsi.Connection.ServiceID = gsi.ServiceID;
                        gsi.Connection.AccountName = ServerUserID;
                        gsi.Connection.SocketKilled += new SocketKilledDelegate(OutboundConnection_SocketKilled);
                        gsi.Connection.SocketConnectionConcluded += new Action<IClientConnection, bool, string>(OutboundConnection_SocketConnectionConcluded);
                        gsi.Connection.EnableUDPKeepAlive = false; // a keep-alive signal is handled manually in the server's own Update loop (this one).

                        // async connect
                        gsi.Connection.BeginConnect(gsi.HostName, gsi.Port, ServerUserID, gsi.SharedHiveKey);
                        gsi.LastUpdate = DateTime.UtcNow;
                        continue;
                    }

                    // Check timeout timers
                    TimeSpan timeSinceLastCheckin = DateTime.UtcNow - gsi.LastUpdate;

                    // If the game server takes more than 30 seconds to respond, it's considered offline.
                    if (timeSinceLastCheckin > TimeSpan.FromMilliseconds(OutboundServerHeartbeatTimer.Interval + 30000))
                    {
                        gsi.IsOnline = false;
                        UpdateOutboundServerAvailability(g.ID);

                        // something's bad.  kill this connection and try to reconnect next round.
                        gsi.Connection.KillConnection("Target server " + gsi.HostName + " (" + gsi.IP + ") not responding.  Recycling this connection.");
                        return;
                    }

                    if (gsi.Connection.ConnectionInProgress)
                    {
                        gsi.IsOnline = false;
                        UpdateOutboundServerAvailability(g.ID);
                        continue;
                    }
                    else if (!gsi.Connection.LoggedIn)
                    {
                        gsi.IsOnline = false;
                        continue;
                    }
                    else // connection is not in progress, i.e. it has completely connected and we are logged in.  send a ping.
                    {
                        // The remote NetworkConnection object is configured to respond to keep alives with a server update (as well as the UDP ACK required for the NAT poke)
                        //gsi.Connection.SendUDPPoke(ListenOnPort);

                        PacketNull ping = gsi.Connection.CreatePacket((int)PacketType.Null, 0, false, false) as PacketNull;
                        ping.NeedsReply = true;
                        gsi.Connection.Send(ping);
                    }
                }
            }

        }    

		/// <summary>
		/// Fires when connection to one of our child servers is lost
		/// </summary>
		protected virtual void OnOutboundServerDisconnected(OutboundServerConnection con, string msg)
		{
		}

        void OutboundConnection_SocketConnectionConcluded(IClientConnection sender, bool result, string msg)
        {
            if (result)
            {
                string rmsg = "";
                ConnectionManager.TrackUserSocket((INetworkConnection)sender, ref rmsg);               
            }
        }

		private void OutboundConnection_SocketKilled(object sender, string msg)
		{
			// Lost a game server.  Clear all of that servers data.  Server will resend that data when we're connected again. This
			// is the preferred way because any number of updates could have happened while we were disconnected.  Better to just
			// resync the whole list.  How big can it be?
			OutboundServerConnection con = sender as OutboundServerConnection;
			if (con == null)
			{
				return;
			}            

			OnOutboundServerDisconnected(con, msg);
            if (con.RemoteEndPoint != null)
            {
                ConnectionManager.RemoveConnection(con.UID);
            }


		}

		#endregion

		/// <summary>
		/// Start accepting connections
		/// </summary>
		public virtual void StartServer()
		{
            Log1.Logger("Initializing encryption library.");
			if (ConfigHelper.GetStringConfig("RunSilverlightPolicyServer").ToLower() == "true")
			{
                string directory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                string filePath = Path.Combine(directory, "SilverlightNetPolicy.xml");                
				if (!File.Exists(filePath))
				{
                    Log1.Logger("Server.Network").Error("Could not find 'SilverlightNetPolicy.xml' at " + filePath + ".  Silverlight policy server has not been started. Silverlight clients will not be able to connect.");
				}
				else
				{
                    Log1.Logger("Server.Network").Info("Starting silverlight net policy server...");
                    PolicyServer = new PolicyServer(filePath);
				}
			}

            if (ConfigHelper.GetStringConfig("RunUnityWebPolicyServer").ToLower() == "true")
			{
                string directory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                string filePath = Path.Combine(directory, "UnityWebNetPolicy.xml");                
				if (!File.Exists(filePath))
				{
                    Log1.Logger("Server.Network").Error("Could not find 'UnityWebNetPolicy.xml' at " + filePath + ".  Unity3d Web policy server has not been started. Unity3d Web clients will not be able to connect.");
				}
				else
				{
                    Log1.Logger("Server.Network").Info("Starting Unity3d Web net policy server...");
                    UnityPolicyServer = new UnityWebPolicyServer(filePath);
				}
			}

            RegisterTypes(typeof(GameObjectScript));
            RegisterTypes(typeof(GenericGameObject));

			StartAcceptingConnections(ConfigHelper.GetIntConfig("ListenOnPort"));
		}

		/// <summary>
		/// Stops listening for connections and closes all active connections.
		/// </summary>
		public virtual bool StopServer()
		{
			StopAcceptingConnections();
            if (m_UDPListener != null)
            {
                m_UDPListener.StopListening();
            }
			KillAllConnections();

            return true;
		}

		/// <summary>
		/// Starts the Async socket m_Listener and sets up a callback to handle the connections as they are made
		/// </summary>
		private void StartAcceptingConnections(int port)
		{
            try
            {
                bool useIP6 = ConfigHelper.GetStringConfig("UseIPv6").ToLower() == "true";                
                // start listening for UDP packets
                int maxSimultaneousReads = ConfigHelper.GetIntConfig("MaxSimultaneousUDPReads", 10);
                if(m_UDPListener != null)
                {
                    m_UDPListener.StopListening();
                }
                
                m_UDPListener = new UDPListener();
                m_UDPListener.StartListening(useIP6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork, port, maxSimultaneousReads,
                    ipe =>
                    {
                        INetworkConnection con = ConnectionManager.GetConnection(ipe);
                        return con;
                    }
                );

                // Listen for TCP connections
                ListenOnPort = port;
                
                // Create the m_Listener socket in this machines IP address
                m_Listener = new Socket(useIP6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                m_Listener.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, DisableTCPDelay);
                m_Listener.Blocking = false;

                if (useIP6)
                {
                    try
                    {
                        m_Listener.SetSocketOption(SocketOptionLevel.IPv6, (SocketOptionName)27, 0);
                        LocalEndPoint = new IPEndPoint(IPAddress.IPv6Any, port);
                        m_Listener.Bind(LocalEndPoint);
                        Log1.Logger("Server.Network").Info("Server accepting connections via IPv4 & IPv6.");
                    }
                    catch(Exception be)
                    {
                        Log1.Logger("Server.Network").Error("Failed binding server listener to IPv6 interface.  Reverting to using IPv4 only.", be);
                        LocalEndPoint = new IPEndPoint(IPAddress.Any, port);
                        m_Listener.Bind(LocalEndPoint);
                    }
                }
                else
                {
                    LocalEndPoint = new IPEndPoint(IPAddress.Any, port);
                    m_Listener.Bind(LocalEndPoint);
                    Log1.Logger("Server.Network").Info("Server accepting connections via IPv4 only.");
                }

                m_Listener.Listen(MaxConnectionBacklog);

                StartAccept();
            }
            catch (Exception e)
            {
                Log1.Logger("Server.Network").Fatal("Unable to start server. " + e.Message, e);
            }
		}

        private bool m_AcceptingConnections = false;
		/// <summary>
		/// Closes down the listener.
		/// </summary>
		private void StopAcceptingConnections()
        {
            m_AcceptingConnections = false;
            Log1.Logger("Server.Network").Info("Stopping listener.  Not longer accepting connection.");
			if (m_Listener != null)
			{
				m_Listener.Close();
			}
		}

		/// <summary>
		/// Begins an operation to accept a connection request from the client
		/// </summary>
		private void StartAccept()
		{
            try
            {
                Log1.Logger("Server.Network").Info("Starting to accept connections on port " + ListenOnPort.ToString() + " with " + MaxSimultaneousAcceptOps.ToString() + " listeners.");
                m_AcceptingConnections = true;
                for (int i = 0; i < MaxSimultaneousAcceptOps; i++)
                {
                    StartAcceptSingle();
                }
            }
            catch (Exception e)
            {
                Log1.Logger("Server.Network").Fatal("Failed to StartAccept connections. " + e.Message, e);
                return;
            }
            
		}

        private void StartAcceptSingle()
        {
            if (!m_AcceptingConnections)
            {
                return;
            }
            SocketAsyncEventArgs acceptEventArg;
            acceptEventArg = SocketAsyncEventArgsCache.PopAcceptEventArg(new EventHandler<SocketAsyncEventArgs>(OnAcceptorActionCompleted), null);
            // don't want a buffer on the initial receive op cause it causes the aceept method to not fire until the first data packet is sent ((SockState)acceptEventArg.UserToken).BufferBlockOffset, 288); // 288 is magic minimum http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.acceptasync.aspx
            acceptEventArg.SetBuffer(null, 0, 0);
            bool willRaiseEvent = m_Listener.AcceptAsync(acceptEventArg);

            if (!willRaiseEvent)
            {
                OnClientAccepted(acceptEventArg);
            }
        }

		/// <summary>
		///This method is the callback method associated with Socket.AcceptAsync 
		///operations and is invoked when an async accept operation completes.
		///This is only when a new connection is being accepted.
		///Notice that Socket.AcceptAsync is returning a value of true, and
		///raising the Completed event when the AcceptAsync method completes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
        private void OnAcceptorActionCompleted(object sender, SocketAsyncEventArgs e)
		{
			//Any code that you put in this method will NOT be called if
			//the operation completes synchronously, which will probably happen when
			//there is some kind of socket error. 
            if (e.LastOperation == SocketAsyncOperation.Accept)
                OnClientAccepted(e);
            else
                OnServiceIdReceived(e);
		}

		/// <summary>
		/// Handle a new socket being accepted.
		/// </summary> 
		/// <param name="acceptEventArgs"></param>
		private void OnClientAccepted(SocketAsyncEventArgs acceptEventArgs)
		{
            try
            {
                // restart listener
                StartAcceptSingle();
                
                // This is when there was an error with the accept op. That should NOT
                // be happening often. It could indicate that there is a problem with
                // that socket. If there is a problem, then we would have an infinite
                // loop here, if we tried to reuse that same socket.
                if (acceptEventArgs.SocketError != SocketError.Success)
                {
                    Log1.Logger("Server.Network").Error("Accept connection incomplete: " + acceptEventArgs.SocketError.ToString());
                    // accept didn't work out. return the cache and dump the socket
                    CloseClientSocket(acceptEventArgs.AcceptSocket);
                    SocketAsyncEventArgsCache.PushAcceptEventArg(acceptEventArgs, new EventHandler<SocketAsyncEventArgs>(OnAcceptorActionCompleted));

                    //Jump out of the method.
                    return;
                }

                if (acceptEventArgs != null && acceptEventArgs.AcceptSocket != null && acceptEventArgs.RemoteEndPoint != null)
                {
                    Log1.Logger("Server.Network").Info("Accepted remote connection from [" + acceptEventArgs.AcceptSocket.RemoteEndPoint.ToString() + "]");
                }
                StartReadServiceId(acceptEventArgs);
            }
            catch (Exception e)
            {
                Log1.Logger("Server.Network").Error("Failed to ProcessAccept connection. " + e.Message, e);
            }
		}

		/// <summary>
		/// Begin the async reading process
		/// </summary>
		/// <param name="args"></param>
        private void StartReadServiceId(SocketAsyncEventArgs args)
		{
            try
            {
                SockState state = (SockState)args.UserToken;

                //Set the buffer for the receive operation.
                state.PacketBuffer = new byte[4];

                // Post async receive operation on the socket.
                Log1.Logger("Server.Network").Debug("Listening for service ID from incoming connection [" + args.AcceptSocket.RemoteEndPoint.ToString() + "].");
                // set the buffer

                args.SetBuffer(SocketAsyncEventArgsCache.BufferBlock, state.BufferBlockOffset, state.BufferBlockLength);
                bool willRaiseEvent = args.AcceptSocket.ReceiveAsync(args);
                if (!willRaiseEvent)
                {
                    OnServiceIdReceived(args);
                }
            }
            catch (Exception e)
            {
                Log1.Logger("Server.Network").Error("Failed to StartReadServiceId. " + e.Message, e);
            }
		}
	
		/// <summary>
		/// Reads the data from the socket and creates a new InboundConnection
		/// </summary>
		/// <param name="args"></param>
		private void OnServiceIdReceived(SocketAsyncEventArgs args)
		{
            Log1.Logger("Server.Network").Debug("OnServiceIdReceived.");
            Socket inboundSocket = args.AcceptSocket;
            try
            {            
                // If there was a socket error, close the connection. This is NOT a normal
                // situation, if you get an error here.
                // In the Microsoft example code they had this error situation handled
                // at the end of ProcessReceive. Putting it here improves readability
                // by reducing nesting some.
                Socket socket = null;
                int flag = -1;
                if (args.SocketError != SocketError.Success || args.BytesTransferred != 4)
                {
                    Log1.Logger("Server.Network").Error("Couldn't read requested serviceID from socket.");
                    CloseClientSocket(args.AcceptSocket);                    
                    return;
                }
                else
                {
                    socket = args.AcceptSocket;
                    SockState state = (SockState)args.UserToken;
                    flag = BitConverter.ToInt32(args.Buffer, state.BufferBlockOffset);                    
                }

                SocketAsyncEventArgsCache.PushAcceptEventArg(args, new EventHandler<SocketAsyncEventArgs>(OnAcceptorActionCompleted));
                InboundConnection con = CreateInboundConnection(socket, this, flag, BlockingMode);
                
            }
            catch (Exception exc)
            {
                CloseClientSocket(inboundSocket);
                Log1.Logger("Server.Network").Error("Failed to Process ServiceID Receive - Inbound connection was not set up. " + exc.Message, exc);
            }
		}   

		/// <summary>
		/// Destroys all inbound and outbound connections.
		/// </summary>
		private void KillAllConnections()
		{
            foreach (GameServerInfoGroup g in OutboundServerGroups.Groups.Values)
            {
                foreach (GameServerInfo<OutboundServerConnection> con in g.OutboundServers)
                {
                    if (con.Connection != null)
                    {
                        con.Connection.KillConnection("Server stopping.");
                    }
                }
            }
			// Revokes all authentication tickets and dumps the connections, if any
			ConnectionManager.NukeAllAuthenticationTickets();
		}

		/// <summary>
		/// Create a connection object for an incoming connections.  Should be overridden.
		/// </summary>
		protected virtual InboundConnection CreateInboundConnection(Socket s, ServerBase server, int serviceID, bool isBlocking)
		{
            if (serviceID == 7) // 7eus
            {
                ZeusInboundConnection zcon = new ZeusInboundConnection(s, server, isBlocking);
                return zcon;
            }

			InboundConnection newClient = new InboundConnection(s, server, isBlocking);
			return newClient;
		}

		/// <summary>
		/// Creates a connection object for an outgoing connection.  outgoing connections for servers are always
		/// to other servers, i.e. 'Outbound Servers'.
		/// </summary>
		/// <returns></returns>
		public virtual OutboundServerConnection CreateOutboundServerConnection(string name, ServerBase server, string reportedIP, int serviceID, bool isBlocking)
		{
			return new OutboundServerConnection(name, server, reportedIP, isBlocking);
		}

		/// <summary>
		/// Shuts down the socket and cleans up resources.
		/// </summary>
        public void CloseClientSocket(Socket e)
        {
            // do a shutdown before you close the socket
            try
            {
                e.Shutdown(SocketShutdown.Send);
            }
            // throws if socket was already closed
            catch (Exception)
            {
            }

            //This method closes the socket and releases all resources, both
            //managed and unmanaged. It internally calls Dispose.           
            e.Close();
        }

        /// <summary>
        /// Relays a packet to a user attached to a remote server, if possible.  If the user is not currently attached, then the message is lost.
        /// </summary>
        /// <param name="targetAccount">the account we're sending this message to</param>
        /// <param name="msg">the packet we want to send</param>
        /// <param name="from">who originated the packet</param>
        public void RelayPacketToRemoteUser(Guid targetAccount, Packet msg, ServerUser from)
        {
            AuthTicket at = ConnectionManager.GetAuthorizationTicketForAccount(targetAccount);
            if (at != null)
            {
                RelayPacketToRemoteUser(at, msg, from);
            }
        }

        /// <summary>
        /// Relays a packet to a user attached to a remote server, if possible.  If the user is not currently attached, then the message is lost.
        /// </summary>
        /// <param name="targetAccount">the character we're sending this message to</param>
        /// <param name="msg">the packet we want to send</param>
        /// <param name="from">who originated the packet</param>
        public void RelayPacketToRemoteUser(int targetCharacter, Packet msg, ServerUser from)
        {
            AuthTicket at = ConnectionManager.GetAuthorizationTicketForCharacter(targetCharacter);
            if (at != null)
            {
                RelayPacketToRemoteUser(at, msg, from);
            }
        }

        /// <summary>
        /// Gets the network connection to a server in the network, presuming we're connected to it.
        /// </summary>
        /// <param name="serverId">unique ID of the server we want to fetch a connection for</param>
        /// <returns></returns>
        public INetworkConnection GetServerConnection(string serverId)
        {
            INetworkConnection con = null;

            // see if it's a inbound connection
            con = ConnectionManager.GetParentConnection(serverId);
            if (con == null)
            {
                // it's not an inbound connection.  see if it's an outbound connection.
                GameServerInfo<OutboundServerConnection> ocon = GetOutboundServerByServerUserID(serverId);
                if (ocon == null)
                {
                    return null;
                }
                con = ocon.Connection;
            }
            return con;
        }

        public void RelayPacketToRemoteUser(AuthTicket target, Packet msg, ServerUser from)
        {
            // Sure it's not local?
            INetworkConnection local = ConnectionManager.GetUserConnection(target.AccountID);
            if (local != null)
            {
                local.Send(msg);
                return;
            }

            // Forward to server
            INetworkConnection remote = GetServerConnection(target.TargetServer);
            if (remote == null)
            {
                // sorry it didn't work out.
                return;
            }

            RelayPacketToRemoteUser(target, msg, from, remote);
        }

        private void RelayPacketToRemoteUser(AuthTicket target, Packet msg, ServerUser from, INetworkConnection con)
        {
            PacketRelay relay = MakeRelayPacket(target.TargetServer, target.AccountID, from, msg);
            con.Send(relay);
        }

        public PacketRelay MakeRelayPacket(string targetServer, Guid targetUser, ServerUser from, Packet message)
        {
            PacketRelay relay = new PacketRelay();
            relay.PacketID = (int)ServerPacketType.Relay;
            relay.PacketSubTypeID = message.PacketID;
            relay.Flags = message.Flags;
            relay.From = from.CurrentCharacter.CharacterInfo;
            relay.Message = message.Serialize(new Pointer());
            relay.OriginServer = this.ServerUserID;
            relay.TargetServer = targetServer;
            relay.To = targetUser;

            return relay;
        }

        /// <summary>
        /// The address for this server as listed in App.Config "ServerAddres"
        /// </summary>
        public string ServerAddress { get; set; }
    }
}
