using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#if !IS_CLIENT
using System.Web.Profile;
using System.Web.Security;
using System.Configuration;
#endif

namespace Shared
{
    /// <summary>
    /// Strongly types access to all of the Account profile properties in the App.Config file.
    /// Subclass this type and be sure to override the TypeHash, Load, Save, Serialize and DeSerialize methods (and
    /// don't forget to call base. on those methods) to add your own properties.
    /// 
    /// When adding properties, you also need to adjust the aspnet_CustomProfile table
    /// in your user database to add new colums for your new data.
    /// 
    /// Finally, adjust the App.Config to reflect the new columns in there.
    /// </summary>
    public class AccountProfile : ISerializableWispObject
    {
        /// <summary>
        /// Stores information about one login session.
        /// </summary>
        public class Session
        {
            public Session(DateTime loginUtc, DateTime logoutUtc, string ip)
            {
                IP = ip;
                LoginUTC = loginUtc;
                LogoutUTC = logoutUtc;
            }

            public string IP { get; set; }
            public DateTime LoginUTC { get; set; }
            public DateTime LogoutUTC { get; set; }
            public TimeSpan Duration 
            {
                get
                {
                    try
                    {
                        return LogoutUTC - LoginUTC;
                    }
                    catch
                    {
                        return TimeSpan.MinValue;
                    }
                }
            }
        }

        public bool IsOnline { get; set; }
        public string[] UserRoles { get; set; }
        public int MaxCharacters { get; set; }
        private DateTime m_CurrentLoginTime = DateTime.MinValue;
        public DateTime CurrentLoginTime
        {
            get
            {
                return m_CurrentLoginTime;
            }
        }

#if !IS_CLIENT                
        public PropertyBag AddedProperties { get; set; }
        public string Username { get; set; }
        public Guid CurrentSessionID { get; set; }
        public  Queue<string>    LoginHistoryIP;
        public  Queue<DateTime>  LoginHistoryTime;
        public  Queue<DateTime>  LogoffHistoryTime;
        public long TotalTimeOnAccount { get; set; }
        public string Alias { get; set; }
        private Guid m_CurrentSessionID = Guid.Empty;        
        private string m_CurrentIP = "";

        public static int MaxSessionsToStore = ConfigHelper.GetIntConfig("MaxLoginSessionsToStore", 20);

        private object m_Lock = new object();
        private long m_TotalSessionTime = 0;
#endif

        public virtual void Serialize(ref byte[] buffer, Pointer p)
        {
#if !IS_CLIENT
            BitPacker.AddString(ref buffer, p, CurrentSessionID.ToString());
            BitPacker.AddString(ref buffer, p, Username);
#endif
            BitPacker.AddInt(ref buffer, p, MaxCharacters);

            // Add roles
            BitPacker.AddInt(ref buffer, p, UserRoles.Length);
            for (int i = 0; i < UserRoles.Length; i++)
            {
                BitPacker.AddString(ref buffer, p, UserRoles[i]);
            }

#if !IS_CLIENT

            // Login IPs
            BitPacker.AddInt(ref buffer, p, LoginHistoryIP.Count);
            for (int i = 0; i < LoginHistoryIP.Count; i++)
            {
                BitPacker.AddString(ref buffer, p, LoginHistoryIP.ElementAt(i));
            }

            // Login Times
            BitPacker.AddInt(ref buffer, p, LoginHistoryTime.Count);
            for (int i = 0; i < LoginHistoryTime.Count; i++)
            {
                BitPacker.AddLong(ref buffer, p, LoginHistoryTime.ElementAt(i).Ticks);
            }

            // Logout Times
            BitPacker.AddInt(ref buffer, p, LogoffHistoryTime.Count);
            for (int i = 0; i < LogoffHistoryTime.Count; i++)
            {
                BitPacker.AddLong(ref buffer, p, LogoffHistoryTime.ElementAt(i).Ticks);
            }

            BitPacker.AddString(ref buffer, p, m_CurrentSessionID.ToString());
            BitPacker.AddLong(ref buffer, p, m_CurrentLoginTime.Ticks);
            BitPacker.AddString(ref buffer, p, m_CurrentIP);
#endif
        }

        public virtual void Deserialize(byte[] data, Pointer p)
        {
#if !IS_CLIENT
            CurrentSessionID = new Guid(BitPacker.GetString(data, p));
            Username = BitPacker.GetString(data, p);
#endif
            MaxCharacters = BitPacker.GetInt(data, p);

            // Roles
            int numRoles = BitPacker.GetInt(data, p);
            UserRoles = new string[numRoles];
            for (int i = 0; i < numRoles; i++)
            {
                UserRoles[i] = BitPacker.GetString(data, p);
            }

#if !IS_CLIENT
            // IPs
            int numIPs = BitPacker.GetInt(data, p);
            LoginHistoryIP = new Queue<string>();
            for (int i = 0; i < numIPs; i++)
            {
                LoginHistoryIP.Enqueue(BitPacker.GetString(data, p));
            }

            // Login times
            int numLogins = BitPacker.GetInt(data, p);
            LoginHistoryTime = new Queue<DateTime>();
            for (int i = 0; i < numLogins; i++)
            {
                LoginHistoryTime.Enqueue(new DateTime(BitPacker.GetLong(data, p), DateTimeKind.Utc));
            }

            // Logoff times
            int numLogouts = BitPacker.GetInt(data, p);
            LogoffHistoryTime = new Queue<DateTime>();
            for (int i = 0; i < numLogouts; i++)
            {
                LogoffHistoryTime.Enqueue(new DateTime(BitPacker.GetLong(data, p), DateTimeKind.Utc));
            }

            // Misc
            m_CurrentSessionID = new Guid(BitPacker.GetString(data, p));
            m_CurrentLoginTime = new DateTime(BitPacker.GetLong(data, p), DateTimeKind.Utc);
            m_CurrentIP = BitPacker.GetString(data, p);
#endif
        }

#if !IS_CLIENT
        public AccountProfile(string username)
#else
        public AccountProfile()
#endif
        {
            IsOnline = false;
            UserRoles = new string[0];            
#if !IS_CLIENT
            AddedProperties = new PropertyBag();
            Username = username;
            LoginHistoryIP = new Queue<string>();
            LoginHistoryTime = new Queue<DateTime>();
            LogoffHistoryTime = new Queue<DateTime>();
            CurrentSessionID = Guid.NewGuid();
#endif
        }

        /// <summary>
        /// Tests to see if the user is in the specified role.  Administrators are in ALL roles.
        /// </summary>
        /// <param name="role">the role to test against</param>
        /// <returns></returns>
        public bool IsUserInRole(string role)
        {
            return Array.IndexOf(UserRoles, "Administrator") > -1 || Array.IndexOf(UserRoles, role) > -1;
        }

#if !IS_CLIENT
        /// <summary>
        /// All of the roles known on the system thus far
        /// </summary>
        public static string[] AllRolesInSystem { get; private set; }

        public virtual void Load(bool serverRequiresAuth)
        {
            if (!serverRequiresAuth)
            {
                // no authorization means no accounts, which means no profiles.
                return;
            }

            if (Username == null || Username.Length < 1)
            {
                return;
            }

            lock (m_Lock)
            {
                UserRoles = Roles.GetRolesForUser(Username);

                // request auth ticket from game server
                ProfileBase profile = ProfileBase.Create(Username, true);

                int maxCharacters = 1;
                string loginHistoryIP = "";
                string loginHistoryTime = "";
                string logoffHistoryTime = "";
                long  totalTimeOnAccount = 0;
                string alias = "";
                byte[] profilePic = new byte[0];

                foreach (SettingsProperty prop in ProfileBase.Properties)
                {
                    switch (prop.Name)
                    {
                        case "MaxCharacters":
                            maxCharacters = (int)profile.GetPropertyValue("MaxCharacters");
                            break;
                        case "LoginHistoryIP":
                            loginHistoryIP = (string)profile.GetPropertyValue("LoginHistoryIP");
                            break;
                        case "LoginHistoryTime":
                            loginHistoryTime = (string)profile.GetPropertyValue("LoginHistoryTime");
                            break;
                        case "LogoffHistoryTime":
                            logoffHistoryTime = (string)profile.GetPropertyValue("LogoffHistoryTime");
                            break;
                        case "TotalTimeOnAccount":
                            totalTimeOnAccount = (long)profile.GetPropertyValue("TotalTimeOnAccount");
                            break;
                        case "Alias":
                            alias = (string)profile.GetPropertyValue("Alias");
                            break;
                        default:                            
                            switch (prop.PropertyType.FullName)
                            {
                                case "System.Int32":
                                    AddedProperties.SetProperty(prop.Name, (int)profile.GetPropertyValue(prop.Name));
                                    break;
                                case "System.Int16":
                                    AddedProperties.SetProperty(prop.Name,  (short)profile.GetPropertyValue(prop.Name));
                                    break;
                                case "System.Bool":
                                    AddedProperties.SetProperty(prop.Name,  (bool)profile.GetPropertyValue(prop.Name));
                                    break;
                                case "System.Single":
                                    AddedProperties.SetProperty(prop.Name,  (float)profile.GetPropertyValue(prop.Name));
                                    break;
                                case "System.Double":
                                    AddedProperties.SetProperty(prop.Name,(double)profile.GetPropertyValue(prop.Name));
                                    break;
                                case "System.Byte":
                                    AddedProperties.SetProperty(prop.Name,  (byte)profile.GetPropertyValue(prop.Name));
                                    break;
                                case "System.DateTime":
                                    AddedProperties.SetProperty(prop.Name,  (DateTime)profile.GetPropertyValue(prop.Name));
                                    break;
                                case "System.Int64":
                                    AddedProperties.SetProperty(prop.Name, (long)profile.GetPropertyValue(prop.Name));
                                    break;
                                case "System.Guid":
                                    AddedProperties.SetProperty(prop.Name, (Guid)profile.GetPropertyValue(prop.Name));
                                    break;
                                case "System.Byte[]":
                                    AddedProperties.SetProperty(prop.Name, (byte[])profile.GetPropertyValue(prop.Name));
                                    break;
                                default:
                                    if (profile.GetPropertyValue(prop.Name) == null)
                                    {
                                        AddedProperties.SetProperty(prop.Name, "");
                                    }
                                    else
                                    {
                                        AddedProperties.SetProperty(prop.Name, (string)profile.GetPropertyValue(prop.Name).ToString());
                                    }
                                    break;
                            }
                            break;
                    }
                }

                MaxCharacters = maxCharacters;
                Alias = alias;
                TotalTimeOnAccount = totalTimeOnAccount;

                if (loginHistoryIP == null || loginHistoryTime == null || logoffHistoryTime == null)
                {
                    Log1.Logger("Server").Warn("Failed to read login history for user [" + Username + "]. If this is the first login, it's not unusual.");
                    return;
                }
                else
                {
                    string[] ips = loginHistoryIP.Trim().Split(char.Parse("|"));
                    string[] logins = loginHistoryTime.Trim().Split(char.Parse("|"));
                    string[] logouts = logoffHistoryTime.Trim().Split(char.Parse("|"));

                    if (ips.Length != logins.Length || logins.Length != logouts.Length)
                    {
                        Log1.Logger("Server").Error("Failed to read login history for user [" + Username + "]. Login history arrays are of inconsistent length.");
                        return;
                    }

                    LoginHistoryIP = new Queue<string>();
                    LogoffHistoryTime = new Queue<DateTime>();
                    LoginHistoryTime = new Queue<DateTime>();

                    Log1.Logger("Server").Debug("Loading [" + ips.Length.ToString() + "] login sessions for profile of [" + Username + "].");
                    for (int i = 0; i < ips.Length; i++)
                    {
                        string ip = ips[i];
                        string login = logins[i];
                        string logout = logouts[i];

                        if (ip.Trim().Length < 1 || login.Trim().Length < 1 || logout.Trim().Length < 1)
                        {
                            continue;
                        }

                        LoginHistoryIP.Enqueue(ip);
                        LogoffHistoryTime.Enqueue(DateTime.Parse(logout));
                        LoginHistoryTime.Enqueue(DateTime.Parse(login));
                    }
                }
            }
        }


        /// <summary>
        /// All login sessions that we are storing.
        /// </summary>
        public IEnumerable<Session> AllSessions
        {
            get
            {
                List<Session> s = new List<Session>();
                lock (m_Lock)
                {                    
                    for (int i = 0; i < LoginHistoryIP.Count; i++)
                    {
                        Session ses = new Session(LoginHistoryTime.ElementAt(i), LogoffHistoryTime.ElementAt(i), LoginHistoryIP.ElementAt(i));
                        s.Add(ses);
                    }
                }
                return s;
            }
        }

        public void SetLoggedIn(string addressFamily, string ip)
        {
            if (ip.Length < 1)
            {
                Log1.Logger("Server").Warn("Tried starting a new play session for [" + Username + "], but their RemoteIP seemed to blank. Session time stamp not recorded.");
                return;
            }

            if (m_CurrentSessionID != Guid.Empty)
            {
                Log1.Logger("Server").Warn("Starting a new play session for [" + Username + "], but there already seems to be one in progress.");
            }

            lock (m_Lock)
            {
                m_CurrentSessionID = CurrentSessionID;
                m_CurrentLoginTime = DateTime.UtcNow;
                m_CurrentIP = ip;
            }
        }

        public void SetLoggedOut()
        {            
            if (m_CurrentSessionID != CurrentSessionID || CurrentSessionID == Guid.Empty ||  m_CurrentLoginTime == DateTime.MinValue)
            {
                return;
            }

            lock (m_Lock)
            {
                if (LoginHistoryIP.Count != LoginHistoryTime.Count || LoginHistoryTime.Count != LogoffHistoryTime.Count)
                {
                    Log1.Logger("Server").Error("Unable to set user's [" + Username + "] profile as logged out.  Login History variables are not the same size.");
                    return;
                }

                
                LoginHistoryIP.Enqueue(m_CurrentIP);
                LoginHistoryTime.Enqueue(m_CurrentLoginTime);
                LogoffHistoryTime.Enqueue(DateTime.UtcNow);

                TimeSpan thisSession = DateTime.UtcNow - m_CurrentLoginTime;
                m_TotalSessionTime += thisSession.Ticks;

                while (LoginHistoryIP.Count > MaxSessionsToStore)
                {
                    LoginHistoryIP.Dequeue();
                    LoginHistoryTime.Dequeue();
                    LogoffHistoryTime.Dequeue();
                }

                m_CurrentSessionID = Guid.Empty;
                m_CurrentLoginTime = DateTime.MinValue;
            }
        }

        public virtual bool Save(bool serverRequiresAuth)
        {
            if (!serverRequiresAuth)
            {
                // no authorization means no accounts, which means no profiles.
                return true;
            }

            if (LoginHistoryIP.Count != LoginHistoryTime.Count || LoginHistoryTime.Count != LogoffHistoryTime.Count)
            {
                Log1.Logger("Server").Error("Unable to save user's ["+Username+"] profile.  Login History variables are not the same size.");
                return false;
            }

            try
            {
                lock (m_Lock)
                {
                    ProfileBase profile = ProfileBase.Create(Username, true);
                    profile.SetPropertyValue("MaxCharacters", MaxCharacters);
                    string[] curRoles = Roles.GetRolesForUser(Username);
                    foreach (string role in UserRoles)
                    {
                        try
                        {
                            if (Array.IndexOf(curRoles, role) == -1)
                            {
                                Roles.AddUserToRole(Username, role);
                            }
                        }
                        catch (Exception e)
                        {
                            Roles.CreateRole(role);
                            Roles.AddUserToRole(Username, role);
                        }
                    }

                    if (LoginHistoryIP.Count != LoginHistoryTime.Count || LoginHistoryTime.Count != LogoffHistoryTime.Count)
                    {
                        Log1.Logger("Server").Error("Unable to save user's [" + Username + "] login history to profile.  Login History arrays are not the same size.");
                    }
                    else
                    {
                        string ips = "";
                        string logins = "";
                        string logouts = "";

                        for (int i = 0; i < LoginHistoryIP.Count; i++)
                        {
                            ips += LoginHistoryIP.ElementAt(i) + "|";
                            logins += LoginHistoryTime.ElementAt(i) + "|";
                            logouts += LogoffHistoryTime.ElementAt(i) + "|";
                        }

                        ips = ips.Trim(char.Parse("|"));
                        logins = logins.Trim(char.Parse("|"));
                        logouts = logouts.Trim(char.Parse("|"));

                        profile.SetPropertyValue("LoginHistoryIP", ips);
                        profile.SetPropertyValue("LoginHistoryTime", logins);
                        profile.SetPropertyValue("LogoffHistoryTime", logouts);

                        long currentTimeSpent = (long)profile.GetPropertyValue("TotalTimeOnAccount");

                        Int64Property pl = AddedProperties.GetProperty("TotalTimeOnAccount") as Int64Property;
                        if (pl != null)
                        {
                            pl.Value = currentTimeSpent + m_TotalSessionTime;
                            Log1.Logger("Account").Info("Updating total account login time for [" + Username + "] from " + new TimeSpan(currentTimeSpent).ToString("g") + " to " + new TimeSpan(currentTimeSpent + m_TotalSessionTime).ToString("g"));
                        }
                        m_TotalSessionTime = 0;

                        foreach (Property p in AddedProperties.AllProperties)
                        {
                            switch (p.Name)
                            {
                                case "LoginHistoryIP":
                                case "LoginHistoryTime":
                                case "LogoffHistoryTime":
                                case "":
                                    continue;
                                default:
                                    profile.SetPropertyValue(p.Name, p.PropertyValue());
                                    break;
                            }

                        }

                        Log1.Logger("Server").Debug("Saving [" + LoginHistoryIP.Count + "] login sessions for profile of [" + Username + "].");
                    }
                    //profile.SetPropertyValue("Alias", Alias);

                    profile.Save();
                    Log1.Logger("Server").Info("Saved profile info [" + Username + "].");
                }
            }
            catch (Exception e)
            {
                Log1.Logger("Server").Error("Failed to save profile for user [" + Username + "]. " + e.Message);
                return false;
            }

            return true;
        }

#endif

        private static uint m_TypeHash = 0;
        public virtual uint TypeHash
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

    }
}
