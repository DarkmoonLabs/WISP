using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO.Compression;
using System.Web.Security;
using System.Web.Profile;
using System.Collections;
using System.Configuration;

namespace Shared
{
    /// <summary>
    /// Represents one user connecting to the server.
    /// </summary>
    public class LSInboundConnection : InboundConnection
    {
        public LSInboundConnection(Socket s, ServerBase server, bool isBlocking)
            : base(s, server, isBlocking)
        {
            RegisterPacketHandler((int)PacketType.LoginRequest, OnPlayerLoginRequest);
        }

        private bool CreateNewAccount(string accountName, string password, string email, ref string msg)
        {
            bool rslt = true;

            // try creating a new account for this user
            try
            {
                MembershipCreateStatus status;
                MembershipUser newUser = Membership.CreateUser(accountName, password, email, null, null, true, out status);
                Log1.Logger("Server").Info("Created new account [" + accountName + "] for [" + RemoteIP + "]");
                if (newUser == null)
                {
                    rslt = false;
                    msg = string.Format("Unable to create new user. " + GetCreateNewUserError(status));
                }

                if (rslt)
                {
                    ServerUser.ID = (Guid)newUser.ProviderUserKey;

                    // just activate the account. we'll create and associate with a character later                    
                    Roles.AddUserToRole(newUser.UserName, "ActiveUser");

                    ServerUser.Profile.Save(MyServer.RequireAuthentication);
                }

                //---------------
            }
            catch (Exception exc)
            {
                Log1.Logger("Server").Error("Failed to create new user account. " + exc.Message);
                rslt = false;
                msg = "Unknow error creating user.";
            }

            return rslt;
        }

        /// <summary>
        /// Phase 1: Player requests login to the system.  This method attempts to authenticate the player (or create a new account, depending on method parms)
        /// </summary>
        public void OnPlayerLoginRequest(INetworkConnection con, Packet pMsg)
        {
            if (!MyServer.RequireAuthentication)
            {
                DoNoAuthLogin(con, pMsg);
            }
            else
            {
                DoDatabaseLogin(con, pMsg);
            }
        }

        protected virtual bool HaveServersForService()
        {
            foreach (GameServerInfoGroup g in MyServer.OutboundServerGroups.Groups.Values)
            {
                if(g.HasLiveOutboundServerConnections)
                {
                    return true;
                }
            }

            return false;
        }

        private void DoNoAuthLogin(INetworkConnection con, Packet pMsg)
        {
            try
            {
                PacketLoginRequest p = pMsg as PacketLoginRequest;
                ServerUser.AccountName = Guid.NewGuid().ToString(); // assign random session name
                ServerUser.OwningServer = MyServer.ServerUserID;
                Log1.Logger("LoginServer.Inbound.Login").Info("No-auth assigned user name " + ServerUser.AccountName + " from " + RemoteIP + " is attempting login...");
                Log1.Logger("LoginServer.UserIPMap").Info("User [" + p.AccountName + "] from [" + RemoteIP + "] is attempting login.");
                string msg = "";

                if (!HaveServersForService())
                {
                    PacketLoginResult ns = (PacketLoginResult)CreatePacket((int)PacketType.LoginResult, 0, true, true);
                    ns.ReplyMessage = "No servers available for service.\r\n\r\nTry again later.";
                    ns.IsCritical = true;
                    ns.ReplyCode = ReplyType.Failure;
                    pMsg.ReplyPacket = ns;
                    return;
                }

                PacketLoginResult result = CreateLoginResultPacket();
                if (result.ReplyCode == ReplyType.OK)
                {
                    ServerUser.AuthTicket = Guid.NewGuid();
                    ServerUser.IsAuthenticated = true;
                    ServerUser.ID = Guid.NewGuid(); // generate random ID for this session

                    RegisterPacketHandler((int)PacketType.RequestHandoffToServer, OnClusterHandoffRequest);
                    ServerUser.Profile.UserRoles = new string[0];
                    result.Parms.SetProperty("AccountName", ServerUser.AccountName);
                    result.Parms.SetProperty(-1, ServerUser.Profile.UserRoles);
                    result.Parms.SetProperty(-2, ServerUser.Profile.MaxCharacters);
                    ConnectionManager.AuthorizeUser(ServerUser);
                }
                pMsg.ReplyPacket = result;
                Log1.Logger("LoginServer.Inbound.Login").Info("Game client *" + ServerUser.AccountName + "* authentication: " + result.ReplyCode.ToString() + ". " + result.ReplyMessage);
            }
            catch (Exception e)
            {
                Log1.Logger("LoginServer.Inbound.Login").Error("Exception thrown whilst player attempted login. " + e.Message, e);
                KillConnection("Error logging in.");
            }
        }

        protected virtual bool CanCreateNewAccount(Packet p, ref string msg)
        {
            return true;
        }

        private void DoDatabaseLogin(INetworkConnection con, Packet pMsg)
        {
            try
            {
                PacketLoginRequest p = pMsg as PacketLoginRequest;
                ServerUser.AccountName = p.AccountName;
                ServerUser.OwningServer = MyServer.ServerUserID;
                Log1.Logger("LoginServer.Inbound.Login").Info("User " + p.AccountName + " from " + RemoteIP + " is attempting login...");
                Log1.Logger("LoginServer.UserIPMap").Info("User [" + p.AccountName + "] from [" + RemoteIP + "] is attempting login.");

                if (!HaveServersForService())
                {
                    PacketLoginResult ns = (PacketLoginResult)CreatePacket((int)PacketType.LoginResult, 0, true, true);
                    ns.ReplyMessage = "No servers available for service.\r\n\r\nTry again later.";
                    ns.IsCritical = true;
                    ns.ReplyCode = ReplyType.Failure;
                    pMsg.ReplyPacket = ns;
                    return;
                }

                string msg = "";
                if (p.IsNewAccount && (!LoginServer.AllowNewAccountsOnTheFly || !CanCreateNewAccount(pMsg, ref msg) || !CreateNewAccount(p.AccountName, p.Password, p.AccountName, ref msg)))
                {
                    if (!LoginServer.AllowNewAccountsOnTheFly)
                    {
                        Log1.Logger("LoginServer.Inboud.Login").Warn("Login packet specified new account request flag but server config disallows this. Check 'AllowNewAccountsOnTheFly' in the config file. Default setting is FALSE.");
                    }
                    PacketLoginResult lrf = (PacketLoginResult)CreatePacket((int)PacketType.LoginResult, 0, true, true);
                    lrf.IsCritical = true; // this along with reply code failure forces D/C
                    lrf.ReplyCode = ReplyType.Failure;
                    lrf.ReplyMessage = msg;
                    pMsg.ReplyPacket = lrf;

                    Log1.Logger("LoginServer.Inbound.Login").Info(p.AccountName + " failed to create new account. " + lrf.ReplyMessage);
                    return;
                }

                bool hasAccountAccess = Membership.ValidateUser(p.AccountName, p.Password);

                if (!hasAccountAccess)
                {
                    PacketLoginResult lrf = (PacketLoginResult)CreatePacket((int)PacketType.LoginResult, 0, true, true);
                    lrf.IsCritical = true; // this along with reply code failure forces D/C
                    lrf.ReplyCode = ReplyType.Failure;
                    lrf.ReplyMessage = string.Format(p.AccountName + " authentication Failed. No account matching these credentials was found. Goodbye.");
                    pMsg.ReplyPacket = lrf;

                    Log1.Logger("LoginServer.Inbound.Login").Info(lrf.ReplyMessage);
                    return;
                }

                ServerUser.Profile.Load(MyServer.RequireAuthentication);

                foreach (AccountProfile.Session s in ServerUser.Profile.AllSessions)
                {
                    System.Diagnostics.Debug.WriteLine(ServerUser.AccountName + ": " + s.LoginUTC.ToLongTimeString() + " to " + s.LogoutUTC.ToLongTimeString() + " (duration " + s.Duration.TotalMinutes + " mins). From " + s.IP);
                }

                ServerUser.Profile.Save(MyServer.RequireAuthentication);

                // check if suspension needs to be lifted
                DateTime suspensionRelease = ServerUser.Profile.AddedProperties.GetDateTimeProperty("SuspensionRelease").GetValueOrDefault(DateTime.MinValue);
                bool isSuspended = false;
                if (suspensionRelease != DateTime.MinValue)
                {
                    isSuspended = true;
                    // currently suspended.  need to lift suspension?
                    if (suspensionRelease >= DateTime.UtcNow)
                    {
                        DB.Instance.User_Unsuspend(ServerUser.ID, "System", suspensionRelease, "Suspension expired.  Time served.", -1);
                        isSuspended = false;
                    }
                }

                if (isSuspended)
                {
                    PacketLoginResult lrf = (PacketLoginResult)CreatePacket((int)PacketType.LoginResult, 0, true, true);
                    lrf.IsCritical = true; // this along with reply code failure forces D/C
                    lrf.ReplyCode = ReplyType.Failure;
                    lrf.ReplyMessage = "[" + p.AccountName + "] is currently suspended until " + suspensionRelease.ToString("g") + " UTC. Please log in to this account through our website for details. Goodbye.";
                    pMsg.ReplyPacket = lrf;

                    Log1.Logger("LoginServer.Inbound.Login").Info(lrf.ReplyMessage);
                    return;
                }

                bool accountIsActive = ServerUser.Profile.IsUserInRole("ActiveUser");
                if (!accountIsActive)
                {
                    PacketLoginResult lrf = (PacketLoginResult)CreatePacket((int)PacketType.LoginResult, 0, true, true);
                    lrf.IsCritical = true; // this along with reply code failure forces D/C
                    lrf.ReplyCode = ReplyType.Failure;
                    lrf.ReplyMessage = p.AccountName + " this account is currently not active.  Please log in to this account through our website for details. Goodbye.";
                    pMsg.ReplyPacket = lrf;
                
                    Log1.Logger("LoginServer.Inbound.Login").Info(lrf.ReplyMessage);
                    return;
                }

                PacketLoginResult result = CreateLoginResultPacket();
                if (result.ReplyCode == ReplyType.OK)
                {
                    ServerUser.AuthTicket = Guid.NewGuid();
                    ServerUser.IsAuthenticated = true;
                    ServerUser.ID = (Guid)Membership.GetUser(p.AccountName).ProviderUserKey;

                    RegisterPacketHandler((int)PacketType.RequestHandoffToServer, OnClusterHandoffRequest);

                    // load the user profile and add it to the packet.                    
                    result.Parms.SetProperty(-1, ServerUser.Profile.UserRoles);
                    result.Parms.SetProperty(-2, ServerUser.Profile.MaxCharacters);
                    ConnectionManager.AuthorizeUser(ServerUser);                   
                }
                
                pMsg.ReplyPacket = result;

                

                Log1.Logger("LoginServer.Inbound.Login").Info("Game client *" + ServerUser.AccountName + "* authentication: " + result.ReplyCode.ToString() + ". " + result.ReplyMessage);
            }
            catch (Exception e)
            {
                Log1.Logger("LoginServer.Inbound.Login").Error("Exception thrown whilst player attempted login. " + e.Message, e);
                KillConnection("Error logging in.");
            }
        }

        /// <summary>
        /// Sends the target connection a listing and status of all game server clusters that this login server knows about.
        /// This is the listing that the client should allow the player too choose from in the UI.
        /// Returns false if currently no servers are available for service.
        /// </summary>
        /// <param name="con"></param>
        protected virtual PacketLoginResult CreateLoginResultPacket()
        {
            // send login result
            PacketLoginResult lr = (PacketLoginResult)CreatePacket((int)PacketType.LoginResult, 0, true, true);
            lr.ReplyCode = ReplyType.OK;

            bool haveAnyOnline = false;
            foreach (GameServerInfoGroup g in MyServer.OutboundServerGroups.Groups.Values)
            {
                if (!haveAnyOnline)
                {
                    haveAnyOnline = g.HasLiveOutboundServerConnections;
                }

                GameServerInfo<OutboundServerConnection> svr = g.NextConnection();

                string serverName = g.ID;
                
                string serverInfo = serverName + "," + g.HasLiveOutboundServerConnections;
                lr.ReplyMessage += "|" + serverInfo;
            }

            if (!haveAnyOnline)
            {
                lr.ReplyMessage = "No servers available for service.";
                lr.IsCritical = true;
                lr.ReplyCode = ReplyType.Failure;
            }

            Log1.Logger("LoginServer.Inbound.Login").Debug("Sending client " + ServerUser.AccountName + " game server info: " + lr.ReplyMessage);
            return lr;
        }

        /// <summary>
        /// Gets called when a player requests to be handed off to a particular Hive.  This Base implementation simply returns the "Next" server
        /// in the indicated server group's connection list, using the group's ConnectMethod to determine the algorithm by which the "Next" 
        /// connection is chosen (I.e. random or roundrobin).  This implementation only considers currently connected servers as candidates.
        /// </summary>
        /// <param name="serverGroup"></param>
        /// <returns></returns>
        protected virtual GameServerInfo<OutboundServerConnection> GetTargetServerForClusterHandoff(string serverGroup)
        {
            GameServerInfoGroup g = MyServer.OutboundServerGroups[serverGroup];
            if (g == null)
            {
                return null;
            }
            return g.NextConnection(); ;
        }

        protected virtual GameServerInfo<OutboundServerConnection> GetServerInfo(string serverName)
        {
            return GetTargetServerForClusterHandoff(serverName);
        }

        /// <summary>
        /// Phase 2: Player requests to play on a specified game server.  This method forwards that request to the game server.
        /// </summary>
        private void OnClusterHandoffRequest(INetworkConnection con, Packet msg)
        {
            PacketRequestHandoffToServerCluster packetRequestHandoffToServer = msg as PacketRequestHandoffToServerCluster;
            if (!ServerUser.IsAuthenticated)
            {
                KillConnection(" [" + ServerUser.AccountName + "] requested server hand off to [" + packetRequestHandoffToServer.TargetServerName + "] without being authenticated.");
            }            

            GameServerInfo<OutboundServerConnection> gsi = GetServerInfo(packetRequestHandoffToServer.TargetServerName); 
            
            if (gsi == null) // cluster offline or at capacity?
            {
                PacketGameServerTransferResult p = (PacketGameServerTransferResult)CreatePacket((int)PacketType.PacketGameServerAccessGranted, 0, true, true);
                p.ReplyMessage = "Game service not currently available.";
                p.ReplyCode = ReplyType.Failure;

                // send an updated listing of online servers
                string servers = "";
                foreach (GameServerInfoGroup gr in MyServer.OutboundServerGroups.Groups.Values)
                {
                    string serverInfo = gr.ID + "," + gr.HasLiveOutboundServerConnections;
                    servers += "|" + serverInfo;
                }

                servers = servers.Trim('|');
                p.Parms.SetProperty((int)PropertyID.ServerListing, servers);

                msg.ReplyPacket = p;
                return;
            }
            
            Log1.Logger("LoginServer.Inbound.Login").Info("Player " + ServerUser.AccountName + " is requesting handoff to game server " + gsi.Name);

            // request auth ticket from game server

            Log1.Logger("LoginServer.Inbound.Login").Debug("Requesting authenticated client *" + ServerUser.AccountName + "* (" + RemoteIP + ") to be handed off to server group " + packetRequestHandoffToServer.TargetServerName + ".");
            ServerUser.TransferToServerUnassisted(gsi.IP, gsi.Port, Guid.Empty, gsi.UserID, gsi.Name);
            //gsi.Connection.RequestPlayerHandoff(ServerUser.ID, ServerUser.AccountName, packetRequestHandoffToServer.TargetResource, ServerUser.Profile, null, "");
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

        protected override void OnSocketKilled(string msg)
        {
            base.OnSocketKilled(msg);
            Log1.Logger("LoginServer.Inbound.Login").Info("Disconnected from " + ServerUser.AccountName + "(" + msg + ") ");
        }
    }
}
