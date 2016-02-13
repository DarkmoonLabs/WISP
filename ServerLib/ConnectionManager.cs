using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;
using System.Net;
using System.Threading;

namespace Shared
{
    /// <summary>
    /// Keeps track of all sockets connected to the server.  Additionally, it tracks user accounts that have been authenticated and handles authentication
    /// ticket expiration and renewal.
    /// </summary>
    public class ConnectionManager
    {
        #region Timer Util

        private static System.Threading.Timer m_Timer;

        private static void SetTimer(int ms)
        {
            if (ms < 1)
            {
                CancelTimer();
                return;
            }

            if (m_Timer == null)
            {
                m_Timer = new System.Threading.Timer(new TimerCallback(OnTimerElapsed), null, ms, Timeout.Infinite);
            }
            else
            {
                CancelTimer();
                m_Timer.Change(ms, Timeout.Infinite);
            }
        }

        private static void CancelTimer()
        {
            if (m_Timer == null)
            {
                return;
            }
            m_Timer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
        }

        #endregion

        static ConnectionManager()
        {
            AuthTicketLifetime = TimeSpan.FromSeconds(ConfigHelper.GetIntConfig("PlayerAuthticketExpirationSecs", 300));
            SetTimer(ConfigHelper.GetIntConfig("PlayerAuthticketExpirationCheckIntervalSecs", 15) * 1000);
        }

        /// <summary>
        /// These are the accounts that can currently play, as they have been authenticated by this server.  They
        /// may or may not be currently connected.
        /// </summary>
        public static TwoKeyDictionary<string, Guid, ServerUser> AuthorizedAccounts = new TwoKeyDictionary<string, Guid, ServerUser>();

        /// <summary>
        /// Each server in the cluster can have parents (a server that dials in to us).  This variable tracks those parents.
        /// </summary>
        public static Dictionary<string, InboundConnection> ParentConnections = new Dictionary<string, InboundConnection>();

        /// <summary>
        /// Thread syncronization object
        /// </summary>
        private static object m_AuthorizedAccountsSyncRoot = new object();

        /// <summary>
        /// Should we nuke all accounts the next time the timer fires?
        /// </summary>
        private static bool m_NukeAllAccounts = false;

        /// <summary>
        /// The amount of time that an auth ticket lives
        /// </summary>
        public static TimeSpan AuthTicketLifetime = TimeSpan.MinValue;

        /// <summary>
        /// Actual connected sockets
        /// </summary>
        public static TwoKeyDictionary<Guid, IPEndPoint, INetworkConnection> Clients = new TwoKeyDictionary<Guid, IPEndPoint, INetworkConnection>();

        /// <summary>
        /// Grabs the parent connection, given a specific server account name.
        /// </summary>
        /// <param name="accountName"></param>
        /// <returns></returns>
        public static InboundConnection GetParentConnection(string accountName)
        {
            InboundConnection con = null;
            if (ParentConnections.TryGetValue(accountName.ToLower(), out con))
            {
                return con;
            }
            return null;
        }

        /// <summary>
        /// Convenience method that checks to see if any parent server is connected.  You might call this method if your cluster configurations only
        /// ever has a single parent connection dialing in, so you don't bother tracking account names
        /// </summary>
        /// <returns>true if there is any parent server that can be communicated with.</returns>
        public static bool IsAnyParentServerConnected()
        {
            if (Clients.Count < 0)
            {
                return false;
            }

            Dictionary<string, InboundConnection>.Enumerator enu = ParentConnections.GetEnumerator();
            while (enu.MoveNext())
            {
                if (enu.Current.Value.IsAlive)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if a given parent server is alive and can receive communications from us
        /// </summary>
        /// <param name="serverAccountName">the account name of the parent server</param>
        /// <returns>true if the server can be communicated with</returns>
        public static bool IsParentServerConnected(string serverAccountName)
        {
            InboundConnection con = null;
            if (ParentConnections.TryGetValue(serverAccountName.ToLower(), out con))
            {
                return con.IsAlive;
            }

            return false;
        }

        /// <summary>
        /// Forcibly revokes all authentication tickets
        /// </summary>
        /// <returns></returns>
        public static int NukeAllAuthenticationTickets()
        {
            int curCon = AuthorizedAccounts.Count;
            m_NukeAllAccounts = true;
            OnTimerElapsed(null);
            curCon = curCon - AuthorizedAccounts.Count; // how many did we nuke?
            return curCon;
        }

        protected static void OnTimerElapsed(object state)
        {
            CancelTimer();

            try
            {
                lock (m_AuthorizedAccountsSyncRoot)
                {
                    IEnumerator<KeyValuePair<string, ServerUser>> enu = AuthorizedAccounts.GetEnumerator();
                    DateTime now = DateTime.UtcNow;
                    List<string> remove = new List<string>();

                    while (enu.MoveNext())
                    {
                        if (!enu.Current.Value.Profile.IsUserInRole("Administrator") && (m_NukeAllAccounts || enu.Current.Value.AuthorizationExpires <= now))
                        {
                            remove.Add(enu.Current.Value.AccountName.ToLower());
                            if (enu.Current.Value.MyConnection != null)
                            {
                                // killing the connection rmeoves it from the tracked client list in the disconnected event handler
                                enu.Current.Value.MyConnection.KillConnection("Auth ticket expired.");
                            }
                        }
                    }

                    for (int i = 0; i < remove.Count; i++)
                    {
                        AuthorizedAccounts.Remove(remove[i]);
                    }
                }
            }
            catch (Exception ex)
            {
            }
            finally
            {
                m_NukeAllAccounts = false;
                SetTimer(ConfigHelper.GetIntConfig("PlayerAuthticketExpirationCheckIntervalSecs", 15) * 1000);
            }
        }

        /// <summary>
        /// Each server in the cluster can have one parent.  Whenever the parent connects, we call this method.
        /// </summary>
        public static void AddParentConnection(InboundConnection client, ServerUser su)
        {
            InboundConnection con = null;
            if (ParentConnections.TryGetValue(client.ServerUser.AccountName.ToLower(), out con))
            {
                con.KillConnection("Parent server logging in again from different connection.");
                con.Disconnected -= new InboundConnection.DisconnectedDelegate(ParentConnection_Disconnected);
                ParentConnections.Remove(con.ServerUser.AccountName.ToLower());
            }
            
            ParentConnections.Add(client.ServerUser.AccountName.ToLower(), client);
            client.Disconnected += new InboundConnection.DisconnectedDelegate(ParentConnection_Disconnected);
        }
         
        public static event InboundConnection.DisconnectedDelegate ParentDisconnected;
        static void ParentConnection_Disconnected(InboundConnection con, string msg)
        {
            ParentConnections.Remove(con.ServerUser.AccountName.ToLower());
            if (ParentDisconnected != null)
            {
                ParentDisconnected(con, msg);
            }
        }

        /// <summary>
        /// Adds a socket connection to be tracked by the server
        /// </summary>
        public static bool TrackUserSocket(INetworkConnection client, ref string msg)
        {
            msg = "";
            if (client == null || client.RemoteEndPoint == null)
            {
                return false;
            }

            if (Clients.ContainsKey(client.RemoteEndPoint as IPEndPoint))
            {
                Clients[client.RemoteEndPoint as IPEndPoint].KillConnection("New connection being made from elsewhere...");
                RemoveConnection(client.RemoteEndPoint as IPEndPoint);
            }

            InboundConnection inb = client as InboundConnection;
            if (inb != null && Clients.Count + 1 > inb.MyServer.MaxInboundConnections)
            {
                msg = "Server is full.";
                return false;
            }

            Clients.Add(client.UID, client);
            Clients.Associate(client.RemoteEndPoint as IPEndPoint, client.UID);
            PerfMon.IncrementCustomCounter("Live Connections", 1);
            return true;
        }

        /// <summary>
        /// Returns the auth ticket for the character, which includes the server they were last seen on.
        /// </summary>
        /// <param name="toonID"></param>
        /// <returns></returns>
        public static AuthTicket GetAuthorizationTicketForCharacter(int toonID)
        {
            AuthTicket at = new AuthTicket();
            Guid ticket = Guid.Empty;
            string authServer = "";
            DateTime whenAuthd = DateTime.MinValue;
            string account = "";
            string targetServerID = "";
            Guid accountID = Guid.Empty;
            if (!DB.Instance.User_GetAuthorizationTicketForCharacter(out account, out authServer, out ticket, out whenAuthd, toonID, out targetServerID, out accountID) || ticket == Guid.Empty)
            {
                return null;
            }

            at.AccountName = account;
            at.AccountID = accountID;
            at.AuthorizedOn = whenAuthd;
            at.AuthorizingServer = authServer;
            at.TargetServer = targetServerID;
            at.CharacterID = toonID;

            return at;
        }

        /// <summary>
        /// Returns the auth ticket for the character, which includes the server they were last seen on.
        /// </summary>
        /// <param name="toonID"></param>
        /// <returns></returns>
        public static AuthTicket GetAuthorizationTicketForAccount(Guid accountID)
        {
            AuthTicket at = new AuthTicket();
            Guid ticket = Guid.Empty;
            string authServer = "";
            DateTime whenAuthd = DateTime.MinValue;
            string targetServerID = "";
            int toonID = -1;
            string account = "";

            if (!DB.Instance.User_GetAuthorizationTicketForAccount(out account, out authServer, out ticket, out whenAuthd, out toonID, out targetServerID, accountID) || ticket == Guid.Empty)
            {
                return null;
            }

            at.AccountName = account;
            at.AccountID = accountID;
            at.AuthorizedOn = whenAuthd;
            at.AuthorizingServer = authServer;
            at.TargetServer = targetServerID;
            at.CharacterID = toonID;

            return at;
        }

        /// <summary>
        /// Returns the authorized user object, or null if the user isn't currently authorized
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public static ServerUser GetAuthorizedUser(string account, ServerBase server, PacketLoginRequest.ConnectionType conType)
        {
            ServerUser u = null;

            if (conType == PacketLoginRequest.ConnectionType.AssistedTransfer)
            {
                lock (m_AuthorizedAccountsSyncRoot)
                {
                    AuthorizedAccounts.TryGetValue(account.ToLower(), out u);
                }
            }

            else if (conType == PacketLoginRequest.ConnectionType.UnassistedTransfer) 
            {
                Guid ticket = Guid.Empty;
                string authServer = "";
                DateTime whenAuthd = DateTime.MinValue;
                int character = -1;
                string targetServerID = "";
                Guid accountID = Guid.Empty;
                if (!DB.Instance.User_GetAuthorizationTicket(account, out authServer, out ticket, out whenAuthd, out character, out targetServerID, out accountID) || ticket == Guid.Empty)
                {
                    return null;
                }

                if (targetServerID != server.ServerUserID)
                {
                    // we weren't authorized to be on this server.
                    Log1.Logger(server.ServerUserID).Error("[" + account + "] attempted unassisted transfer to [" + server.ServerUserID + "], but that user was only authorized to transfer to target server ID [" + targetServerID+ "]. Connection denied.");
                    return null;
                }

                if (whenAuthd + AuthTicketLifetime < DateTime.UtcNow)
                {
                    // ticket expired.
                    Log1.Logger(server.ServerUserID).Error("[" + account + "] attempted unassisted transfer to [" + server.ServerUserID + "], but that user's auth ticket is expired. Connection denied.");
                    return null;
                }

                // Got a ticket.  Load up the user from the DB.
                u = new ServerUser();

                u.OwningServer = server.ServerUserID;
                u.AuthTicket = ticket;
                u.ID = accountID;
                u.AccountName = account;
                

                // load the profile
                AccountProfile ap = new AccountProfile(account);
                u.Profile = ap;                
                ap.Load(server.RequireAuthentication);

                // load the character
                if (character > -1)
                {
                    string msg = "";
                    u.CurrentCharacter = CharacterUtil.Instance.LoadCharacter(u, character, ref msg);
                    if (u.CurrentCharacter == null)
                    {
                        // Couldn't load character.
                        Log1.Logger(server.ServerUserID).Error("[" + account + "] attempted unassisted transfer with characer [" + character + "], but that character could not be loaded from the DB: [" + msg + "]. Connection denied.");
                        return null;
                    }
                    u.CurrentCharacter.OwningAccount = u;
                    CharacterCache.CacheCharacter(u.CurrentCharacter, server.ServerUserID);
                }                
            }

            AuthorizeUser(u); // gotta call this to activate/renew the auth ticket on this server.
            return u;
        }

             /// <summary>
        /// Adds the user's account to the authorized users list.  This method should also disconnect any previous sockets that might be connected using the same account
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static bool AuthorizeUser(ServerUser client)
        {
            return AuthorizeUser(client, ServerBase.UseDatabaseConnectivity);
        }

        /// <summary>
        /// Adds the user's account to the authorized users list.  This method should also disconnect any previous sockets that might be connected using the same account
        /// </summary>
        /// <param name="client"></param>
        /// <param name="persist">Determines if the ticket should be persisted in the datastore.  Normally, you only need to do this when you are about to transfer control of the player to another server in the Hive.</param>
        /// <returns></returns>
        public static bool AuthorizeUser(ServerUser client, bool persist)
        {
            if (client == null)
            {
                return false;
            }

            lock (m_AuthorizedAccountsSyncRoot)
            {
                if (client.MyConnection != null && client.MyConnection.IsAlive)
                {
                    client.Profile.SetLoggedIn(client.MyConnection.RemoteEndPoint.AddressFamily.ToString(), client.MyConnection.RemoteEndPoint.ToString());
                }
                ServerUser existing = null;
                if (AuthorizedAccounts.TryGetValue(client.AccountName.ToLower(), out existing))
                {
                    UnAuthorizeUser(existing);
                    if (existing.MyConnection != null)
                    {
                        existing.MyConnection.KillConnection("Logging in from another client.");
                    }
                }

                Log1.Logger("Server.Login").Debug("Authorizing *" + client.AccountName.ToLower() + "*");
                AuthorizedAccounts.Add(client.AccountName.ToLower(), client);
                AuthorizedAccounts.Associate(client.ID, client.AccountName.ToLower());
                client.RenewAuthorizationTicket(persist);
            }
            return true;
        }

        public static void UnAuthorizeUser(ServerUser client, bool persist)
        {
            lock (m_AuthorizedAccountsSyncRoot)
            {
                if (client.AccountName == null || client.ID == null)
                {
                    return;
                }

                // Remove from local authorized clients list
                AuthorizedAccounts.Remove(client.AccountName.ToLower());

                // if we persist the unauthorization, the client will have to go through the login server again. 
                if (persist)
                {
                    DB.Instance.User_UnauthorizeSession(client.AccountName);
                }
                Log1.Logger("Server").Debug("Unauthorized account [" + client.AccountName + "] and auth ticket [" + client.AuthTicket.ToString() + "]. [" + AuthorizedAccounts.Count.ToString() + "] authorized accounts left in cache.");
            }
        }

        public static void UnAuthorizeUser(ServerUser client)
        {
            UnAuthorizeUser(client, true);
        }

        /// <summary>
        /// Removes a connection from the list of tracked clients.  This method does not disconnect the user.  To disconnect
        /// the user call GetUser(ip).KillConnection()
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        public static bool RemoveConnection(IPEndPoint remote)
        {
            INetworkConnection con = null;
            if (!Clients.TryGetValue(remote, out con))
            {
                return false;
            }

            if (con == null)
            {
                return false;
            }

            InboundConnection inb = con as InboundConnection;
            if (inb != null && inb.ServerUser != null)
            {
                inb.ServerUser.MyConnection = null;
            }

            Log1.Logger("Server.Login").Info("Untracking socket " + remote.ToString());

            bool haveIt = false;
            if (Clients.ContainsKey(remote))
            {
                haveIt = true;
                PerfMon.IncrementCustomCounter("Live Connections", -1);
            }

            Clients.Remove(remote);
            return haveIt;
        }

        public static bool RemoveConnection(Guid conId)
        {
            INetworkConnection con = null;
            if (!Clients.TryGetValue(conId, out con))
            {
                return false;
            }

            if (con == null)
            {
                return false;
            }

            InboundConnection inb = con as InboundConnection;
            if (inb != null && inb.ServerUser != null)
            {
                inb.ServerUser.MyConnection = null;
            }

            Log1.Logger("Server.Login").Info("Untracking socket " + conId.ToString());

            bool haveIt = false;
            if (Clients.ContainsKey(conId))
            {
                haveIt = true;
                PerfMon.IncrementCustomCounter("Live Connections", -1);
            }

            Clients.Remove(conId);
            return haveIt;
        }
        
        /// <summary>
        /// Grabs a connection from the sockets being tracked list
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static INetworkConnection GetConnection(IPEndPoint remote)
        {
            INetworkConnection con = null;
            Clients.TryGetValue(remote, out con);
            return con;
        }


        /// <summary>
        /// Grabs a connection from the sockets being tracked list
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static INetworkConnection GetConnection(Guid conId)
        {
            INetworkConnection con = null;
            Clients.TryGetValue(conId, out con);
            return con;
        }

        public static INetworkConnection GetUserConnection(Guid user)
        {
            INetworkConnection con = null;
            ServerUser su = null;
            if (AuthorizedAccounts.TryGetValue(user, out su))
            {
                con = su.MyConnection;
            }
            return con;
            
        }


        public delegate int RequestConnectionCountDelegate(int actualConnections);

        /// <summary>
        /// Fires whenever ConnectionManager.ConnectionCount is queried.  Hook into this event to modify the connection count that is returned.
        /// This is Useful if you want to pad the Connection count for any reason.
        /// </summary>
        public static event RequestConnectionCountDelegate RequestConnectionCount;

        /// <summary>
        /// The number of sockets currently connected to the server. In some Hives, this number may be padded for various reason (most often, reserved seats).
        /// This is the property you want to use for purposes of determining if you can accept more connections.  Use ConnectionCount if you want to see
        /// how many clients are currently connected, not counting for any padding.
        /// </summary>
        public static int PaddedConnectionCount
        {
            get
            {
                if (RequestConnectionCount != null)
                {
                    int num = RequestConnectionCount(Clients.Count);
                    if (num < Clients.Count)
                    {
                        // always return AT LEAST the number of actually connected clients
                        return Clients.Count;
                    }
                    return num;
                }
                return Clients.Count;
            }
        }

        /// <summary>
        /// The actual number of clients currently connected.
        /// </summary>
        public static int ConnectionCount
        {
            get
            {
                return Clients.Count;
            }
        }

    }
}
