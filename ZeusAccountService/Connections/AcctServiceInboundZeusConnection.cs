using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Configuration;
using ServerLib;
using System.Web.Security;

namespace Shared
{
    /// <summary>
    /// Represents one other server in the Hive connecting to this server.
    /// </summary>
    public class AcctServiceInboundZeusConnection : ZeusInboundConnection
    {
        public AcctServiceInboundZeusConnection(Socket s, ServerBase server, bool isBlocking)
            : base(s, server, isBlocking)
        {
            // Register Packet handlers that should be accepted for all connection, regardless if they are authenticated or not.
            // use "RegisterPacketHandler(..."
        }

        /// <summary>
        /// Override any methods you like to catch various events.  OnSocketKilled is a common one. OnClusterServerLoginResolved is also a commonly used one.
        /// </summary>
        /// <param name="msg"></param>
        protected override void OnSocketKilled(string msg)
        {
            base.OnSocketKilled(msg);
        }

        private void GetAllDBUsers(WispUsersInfo users, int page, int numPerPage)
        {
            SqlMembershipProvider prov = Membership.Provider as SqlMembershipProvider;

            users.UserDataStore = "Database";
            int totalRecords = 0;
            MembershipUserCollection allUsers = null;
            if (numPerPage == -1 && page == -1)
            {
                allUsers = Membership.GetAllUsers();
                users.TotalUserCount = allUsers.Count;
            }
            else
            {
                allUsers = Membership.GetAllUsers(page, numPerPage, out totalRecords);
                users.TotalUserCount = totalRecords;
            }

            foreach (MembershipUser u in allUsers)
            {
                //string[] roles = Roles.GetRolesForUser(u.UserName);
                users.Users.Add(new WispUsersInfo.User(u.UserName, new string[0], u.IsLockedOut, (Guid)u.ProviderUserKey, u.Email, u.LastLoginDate));
            }
        }

        private WispUsersInfo SearchDBUsers(string name, string email, Guid id, string ip, int page, int numPerPage)
        {
            int total = 0;

            WispUsersInfo wu = new WispUsersInfo();
            WispUsersInfo rslt = new WispUsersInfo();
            try
            {
                MembershipUserCollection users = null;
                if (name.Length > 0)
                {
                    if (!name.EndsWith("%"))
                    {
                        name += "%";
                    }
                    if (!name.StartsWith("%"))
                    {
                        name = "%" + name;
                    }
                    users = Membership.FindUsersByName(name);
                    foreach (MembershipUser u in users)
                    {
                        string[] roles = new string[0];// Roles.GetRolesForUser(u.UserName);
                        wu.Users.Add(new WispUsersInfo.User(u.UserName, roles, u.IsLockedOut, (Guid)u.ProviderUserKey, u.Email, u.LastLoginDate.ToUniversalTime()));
                    }
                }

                if (email.Length > 0)
                {
                    if (!email.EndsWith("%"))
                    {
                        email += "%";
                    }
                    if (!email.StartsWith("%"))
                    {
                        email = "%" + email;
                    }
                    users = Membership.FindUsersByEmail(email);
                    foreach (MembershipUser u in users)
                    {
                        string[] roles = new string[0];// Roles.GetRolesForUser(u.UserName);
                        wu.Users.Add(new WispUsersInfo.User(u.UserName, roles, u.IsLockedOut, (Guid)u.ProviderUserKey, u.Email, u.LastLoginDate));
                    }
                }

                if (id != Guid.Empty)
                {
                    MembershipUser usr = Membership.GetUser(id, false);
                    if (usr != null)
                    {
                        string[] roles = new string[0];// Roles.GetRolesForUser(u.UserName);
                        wu.Users.Add(new WispUsersInfo.User(usr.UserName, roles, usr.IsLockedOut, (Guid)usr.ProviderUserKey, usr.Email, usr.LastLoginDate));
                    }
                }

                if (ip.Length > 0)
                {
                    if (!ip.EndsWith("%"))
                    {
                        ip += "%";
                    }
                    if (!ip.StartsWith("%"))
                    {
                        ip = "%" + ip;
                    }

                    List<WispUsersInfo.User> ipusers = new List<WispUsersInfo.User>();
                    DB.Instance.User_SearchByIP(ip, ipusers);
                    wu.Users.AddRange(ipusers);
                }

                rslt.TotalUserCount = wu.Users.Count;

                // Partition
                IEnumerable<IEnumerable<WispUsersInfo.User>> usrs = wu.Users.Partition(numPerPage);

                // Move to selected page
                IEnumerator<IEnumerable<WispUsersInfo.User>> enu = usrs.GetEnumerator();
                int cur = 0;
                while (enu.MoveNext())
                {
                    if (cur == page)
                    {
                        foreach (WispUsersInfo.User ai in enu.Current)
                        {
                            rslt.Users.Add(ai);
                        }
                    }

                    cur++;
                }
            }
            catch { }


            return rslt;
        }

        /// <summary>
        /// Generates a human error message from a new account failure status code
        /// </summary>
        private static string GetCreateNewUserError(MembershipCreateStatus status)
        {
            switch (status)
            {
                case MembershipCreateStatus.DuplicateUserName:
                    return string.Format("Username already exists. Please enter a different user name.");

                case MembershipCreateStatus.DuplicateEmail:
                    return string.Format("A username for that e-mail address already exists. Please enter a different e-mail address.");

                case MembershipCreateStatus.InvalidPassword:
                    return string.Format("The password provided is invalid. Please enter a valid password value.");

                case MembershipCreateStatus.InvalidEmail:
                    return string.Format("The e-mail address provided is invalid. Please check the value and try again.");

                case MembershipCreateStatus.InvalidAnswer:
                    return string.Format("The password retrieval answer provided is invalid. Please check the value and try again.");

                case MembershipCreateStatus.InvalidQuestion:
                    return string.Format("The password retrieval question provided is invalid. Please check the value and try again.");

                case MembershipCreateStatus.InvalidUserName:
                    return string.Format("The user name provided is invalid. Please check the value and try again.");

                case MembershipCreateStatus.ProviderError:
                    return string.Format("The authentication provider returned an error. Please verify your entry and try again. If the problem persists, please contact your system administrator.");

                case MembershipCreateStatus.UserRejected:
                    return string.Format("The user creation request has been canceled. Please verify your entry and try again. If the problem persists, please contact your system administrator.");

                default:
                    return string.Format("An unknown error occurred. Please verify your entry and try again. If the problem persists, please contact your system administrator.");
            }
        }

        private MembershipUser CreateNewAccount(string accountName, string password, string email, string[] roles, ref string msg)
        {
            bool rslt = true;
            MembershipUser newUser = null;
            // try creating a new account for this user
            try
            {
                MembershipCreateStatus status;
                newUser = Membership.CreateUser(accountName, password, email, null, null, true, out status);
                if (newUser == null)
                {
                    rslt = false;
                    msg = string.Format("Unable to create new user. " + GetCreateNewUserError(status));
                }

                if (rslt)
                {
                    ServerUser.ID = (Guid)newUser.ProviderUserKey;
                    AccountProfile prof = new AccountProfile(newUser.UserName);
                    prof.UserRoles = roles;

                    if (roles == null || roles.Length < 1 || Array.IndexOf(roles, "ActiveUser") == -1)
                    {
                        string[] rolesEx = null;
                        if (roles != null)
                        {
                            rolesEx = new string[roles.Length + 1];
                            Array.Copy(roles, rolesEx, roles.Length);
                        }
                        else
                        {
                            rolesEx = new string[1];
                        }
                        rolesEx[rolesEx.Length - 1] = "ActiveUser";
                        prof.UserRoles = rolesEx;
                    }

                    prof.Save(MyServer.RequireAuthentication);
                }

                //---------------
            }
            catch (Exception exc)
            {
                rslt = false;
                msg = "Unknow error creating user.";
            }

            return newUser;
        }

        private void OnUserCreateRequest(INetworkConnection con, Packet r)
        {
            Log1.Logger("Zeus.").Debug("User creation request from " + ServerUser.AccountName + ".");

            PacketGenericMessage msg = r as PacketGenericMessage;

            string name = msg.Parms.GetStringProperty(2).Trim();
            string email = msg.Parms.GetStringProperty(3).Trim();
            string password = msg.Parms.GetStringProperty(4).Trim();
            string[] roles = msg.Parms.GetStringArrayProperty(5);

            r.ReplyPacket = CreateStandardReply(r, ReplyType.OK, "");
            r.ReplyPacket.Parms.SetProperty(1, MyServer.ServerUserID);
            r.ReplyPacket.Parms.SetProperty(2, name);
            r.ReplyPacket.Parms.SetProperty(3, email);

            List<string> oroles = new List<string>();
            for (int i = 0; i < roles.Length; i++)
            {
                if (roles[i].Trim().Length > 0)
                {
                    oroles.Add(roles[i].Trim());
                }
            }

            string omsg = "";
            MembershipUser newUser = CreateNewAccount(name, password, email, oroles.ToArray(), ref omsg);
            if (newUser == null)
            {
                r.ReplyPacket.ReplyCode = ReplyType.Failure;
                r.ReplyPacket.ReplyMessage = omsg;
            }
            else
            {
                r.ReplyPacket.Parms.SetProperty(4, (Guid)newUser.ProviderUserKey);
            }
        }

        private void OnUserSearch(INetworkConnection con, Packet r)
        {
            Log1.Logger("Zeus").Debug("User search request from " + ServerUser.AccountName + ".");

            PacketGenericMessage msg = r as PacketGenericMessage;

            string name = msg.Parms.GetStringProperty(2).Trim();
            string email = msg.Parms.GetStringProperty(3).Trim();
            Guid guid = msg.Parms.GetGuidProperty(4);
            int page = msg.Parms.GetIntProperty(5).GetValueOrDefault(0);
            int pageSize = msg.Parms.GetIntProperty(6).GetValueOrDefault(0);
            string ip = msg.Parms.GetStringProperty(7).Trim();

            WispUsersInfo users = new WispUsersInfo();
            users.AllowRemoteConnections = ZeusServer.AllowRemote;

            r.ReplyPacket = CreateStandardReply(r, ReplyType.OK, "");
            r.ReplyPacket.Parms.SetProperty(1, MyServer.ServerUserID);
            r.ReplyPacket.Parms.SetProperty(2, users);
            r.ReplyPacket.Parms.SetProperty(3, name);
            r.ReplyPacket.Parms.SetProperty(4, email);
            r.ReplyPacket.Parms.SetProperty(5, guid);
            r.ReplyPacket.Parms.SetProperty(6, page);
            r.ReplyPacket.Parms.SetProperty(7, pageSize);
            r.ReplyPacket.Parms.SetProperty(8, ip);
            users.TotalUserCount = -1;
            if (MyServer.RequireAuthentication)
            {
                r.ReplyPacket.Parms.SetProperty(2, SearchDBUsers(name, email, guid, ip, page, pageSize));
            }
        }

        private void OnUserOverviewRequest(INetworkConnection con, Packet r)
        {
            Log1.Logger("Zeus").Debug("User overview request from " + ServerUser.AccountName + ".");

            PacketGenericMessage msg = r as PacketGenericMessage;
            int pageNum = msg.Parms.GetIntProperty(2).GetValueOrDefault(0);
            int pageSize = msg.Parms.GetIntProperty(3).GetValueOrDefault(50);

            if (!ServerUser.Profile.IsUserInRole("ActiveCustomerService"))
            {
                Log1.Logger("Server.Commands").Warn("[" + ServerUser.AccountName + "] has insufficient permissions to request user info.");
                r.ReplyPacket = CreateStandardReply(r, ReplyType.Failure, "Insufficient permissions. Only Administrators can request Zeus users.");
                return;
            }
           
            WispUsersInfo users = new WispUsersInfo();
            users.AllowRemoteConnections = ZeusServer.AllowRemote;

            r.ReplyPacket = CreateStandardReply(r, ReplyType.OK, "");
            r.ReplyPacket.Parms.SetProperty(1, MyServer.ServerUserID);
            r.ReplyPacket.Parms.SetProperty(2, users);
            users.TotalUserCount = -1;

            if (MyServer.RequireAuthentication)
            {
                int totalUsers = 0;
                Membership.GetAllUsers(0, 1, out totalUsers);
                users.TotalUserCount = totalUsers;
                users.UserDataStore = "Database";
            }
        
        }

        private void OnCharacterDetailRequest(INetworkConnection con, Packet r)
        {
            Log1.Logger("Zeus.Inbound.Client").Debug("Character detail request from " + ServerUser.AccountName + ".");

            PacketGenericMessage msg = r as PacketGenericMessage;
            int id = msg.Parms.GetIntProperty(2).GetValueOrDefault(-1);
            WispCharacterDetail ci = new WispCharacterDetail(id);
            string rmsg = "";

            ServerUser su = new Shared.ServerUser();
            su.ID = Guid.Empty;
            ServerCharacterInfo sci = null;
            if (MyServer.RequireAuthentication)
            {
                sci = CharacterUtil.Instance.LoadCharacter(su, id, ref rmsg);
            }

            r.ReplyPacket = CreateStandardReply(r, ReplyType.OK, "");
            r.ReplyPacket.ReplyMessage = rmsg;

            if (sci == null)
            {
                r.ReplyPacket.ReplyCode = ReplyType.Failure;
                r.ReplyPacket.ReplyMessage = "Character not found. " + rmsg;
            }
            else
            {
                ci.CharacterName = sci.CharacterInfo.CharacterName;
                ci.ID = sci.ID;
                ci.LastLogin = sci.LastLogin;
                ci.Properties = sci.Properties;
                ci.Stats = sci.Stats;

                r.ReplyPacket.Parms.SetProperty(2, ci as ISerializableWispObject);
            }

            r.ReplyPacket.Parms.SetProperty(1, MyServer.ServerUserID);
        }

        private void OnUserDetailRequest(INetworkConnection con, Packet r)
        {
            Log1.Logger("Zeus.Inbound.Client").Debug("User detail request from " + ServerUser.AccountName + ".");

            PacketGenericMessage msg = r as PacketGenericMessage;
            Guid user = msg.Parms.GetGuidProperty(2);

            WispUserDetail ud = new WispUserDetail();
            r.ReplyPacket = CreateStandardReply(r, ReplyType.OK, "");
            r.ReplyPacket.Parms.SetProperty(1, MyServer.ServerUserID);
            r.ReplyPacket.Parms.SetProperty(2, ud);

            MembershipUser usr = Membership.GetUser(user, false);
            if (usr == null)
            {
                r.ReplyPacket.ReplyCode = ReplyType.Failure;
                r.ReplyPacket.ReplyMessage = "User not found.";
            }
            else
            {
                ud.Email = usr.Email;
                ud.ID = user;
                ud.IsApproved = usr.IsApproved;
                ud.IsLocked = usr.IsLockedOut;
                ud.LastLogin = usr.LastLoginDate.ToUniversalTime();
                ud.LastPasswordChange = usr.LastPasswordChangedDate.ToUniversalTime();
                ud.Roles.AddRange(Roles.GetRolesForUser(usr.UserName));
                ud.Username = usr.UserName;
                ud.UserSince = usr.CreationDate;

                ServerUser su = ConnectionManager.GetAuthorizedUser(usr.UserName, MyServer, PacketLoginRequest.ConnectionType.AssistedTransfer);
                ud.IsOnline = su != null;
                if (su != null)
                {
                    ud.CurrentLoginTime = su.Profile.CurrentLoginTime;
                }

                AccountProfile prof = null;
                prof = new AccountProfile(usr.UserName);
                prof.Load(MyServer.RequireAuthentication);

                ud.AddedProperties = prof.AddedProperties;
                ud.LoginSessions = new List<AccountProfile.Session>(prof.AllSessions);

                if (!DB.Instance.User_GetServiceLogEntries(user, "", ud.ServiceNotes))
                {
                    ServiceLogEntry sle = new ServiceLogEntry();
                    sle.EntryBy = "System";
                    sle.Note = "Unable to retrieve service log entries from database.";
                    ud.ServiceNotes.Add(sle);
                }

                ud.Characters = CharacterUtil.Instance.GetCharacterListing(user);
            }
        }

        private void OnUserAddServiceNoteRequest(INetworkConnection con, Packet r)
        {
            Log1.Logger("Zeus.Inbound.Client").Debug("Add service note request from " + ServerUser.AccountName + ".");

            PacketGenericMessage msg = r as PacketGenericMessage;

            Guid account = msg.Parms.GetGuidProperty(2);
            string note = msg.Parms.GetStringProperty(3);

            if (note.Trim().Length < 1)
            {
                r.ReplyPacket.ReplyMessage = "Can't add a blank message.";
                r.ReplyPacket.ReplyCode = ReplyType.Failure;
                return;
            }

            r.ReplyPacket = CreateStandardReply(r, ReplyType.OK, "");
            r.ReplyPacket.Parms.SetProperty(1, MyServer.ServerUserID);
            r.ReplyPacket.Parms.SetProperty(2, account);

            if (!DB.Instance.User_CreateServiceLogEntry(account, "Service Note", ServerUser.AccountName, note, -1))
            {
                r.ReplyPacket.ReplyCode = ReplyType.Failure;
                r.ReplyPacket.ReplyMessage = "Database error while trying to add note.";
            }
        }

        protected override void LoggedInAndReady()
        {
            base.LoggedInAndReady();

            RegisterPacketHandler((int)PacketType.PacketGenericMessage, (int)GenericZeusPacketType.RequestUserOverview, OnUserOverviewRequest);
            RegisterPacketHandler((int)PacketType.PacketGenericMessage, (int)GenericZeusPacketType.RequestSearchUsers, OnUserSearch);
            RegisterPacketHandler((int)PacketType.PacketGenericMessage, (int)GenericZeusPacketType.RequestCreateNewUser, OnUserCreateRequest);
            RegisterPacketHandler((int)PacketType.PacketGenericMessage, (int)GenericZeusPacketType.RequestUserDetail, OnUserDetailRequest);
            RegisterPacketHandler((int)PacketType.PacketGenericMessage, (int)GenericZeusPacketType.AddServiceNote, OnUserAddServiceNoteRequest);
            RegisterPacketHandler((int)PacketType.PacketGenericMessage, (int)GenericZeusPacketType.RequestCharacterDetail, OnCharacterDetailRequest);

        }
    }
}
