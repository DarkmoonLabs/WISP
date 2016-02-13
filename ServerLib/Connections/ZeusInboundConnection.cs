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
using ServerLib;
using log4net.Repository.Hierarchy;
using log4net.Appender;
using log4net;
using log4net.Core;
using Zeus;
using Microsoft.Win32;
using System.IO;

namespace Shared
{
    /// <summary>
    /// Represents one user connecting to the server.
    /// </summary>
    public class ZeusInboundConnection : InboundConnection
    {
        public ZeusInboundConnection(Socket s, ServerBase server, bool isBlocking)
            : base(s, server, isBlocking)
        {
            RegisterPacketHandler((int)PacketType.LoginRequest, OnLoginRequest);
            RegisterPacketHandler((int)PacketType.Null, OnPing);
        }

        private void OnPing(INetworkConnection con, Packet ping)
        {
            //Log1.Logger("Zeus.Inbound.Client").Debug("Sending heartbeat to client " + ((InboundConnection)con).ServerUser.AccountName + ".");
            PacketServerUpdate pong = (PacketServerUpdate)CreatePacket((int)ServerPacketType.ServerStatusUpdate, 0, false, false);
            pong.ServerName = MyServer.ServerName;
            pong.UserID = MyServer.ServerUserID;
            pong.MaxPlayers = MyServer.MaxInboundConnections;
            pong.CurrentPlayers = ConnectionManager.PaddedConnectionCount;
            ping.ReplyPacket = pong;
        }

        #region Config

        private void OnConfigRequest(INetworkConnection con, Packet r)
        {
            Log1.Logger("Zeus").Debug("Config listing request from " + ServerUser.AccountName + ".");

            WispConfigSettings cfg = new WispConfigSettings();
            cfg.Configs = MyServer.AppConfig;
            PacketReply rep = CreateStandardReply(r, ReplyType.OK, "");
            rep.Parms.SetProperty(1, cfg);
            r.ReplyPacket = rep;
        }

        private void OnConfigSaveRequest(INetworkConnection con, Packet r)
        {
            Log1.Logger("Zeus").Debug("Config save request from " + ServerUser.AccountName + ".");
            Log1.Logger("Zeus").Info("[" + ServerUser.AccountName + "] attempting to save server config...");

            if (!ServerUser.Profile.IsUserInRole("Administrator"))
            {
                Log1.Logger("Zeus").Warn("[" + ServerUser.AccountName + "] has insufficient permissions to save configs.");
                r.ReplyPacket = CreateStandardReply(r, ReplyType.Failure, "Insufficient permissions. Only Administrators can update configs.");
                return;
            }

            PacketGenericMessage msg = r as PacketGenericMessage;
            WispConfigSettings cfg = msg.Parms.GetWispProperty(1) as WispConfigSettings;
            Dictionary<string, string> newConfigs = cfg.Configs;

            string msgs = "";
            bool rsult = MyServer.SaveConfigs(newConfigs, true, false, ref msgs);

            r.ReplyPacket = CreateStandardReply(r, rsult ? ReplyType.OK : ReplyType.Failure, msgs);
        }

        #endregion    

        #region Logs

        private void OnLogRequest(INetworkConnection con, Packet r)
        {            
            PacketGenericMessage msg = r as PacketGenericMessage;
            Log1.Logger("Zeus").Debug("Log request from " + ServerUser.AccountName);

            r.ReplyPacket = CreateStandardReply(r, ReplyType.OK, "");
            r.ReplyPacket.IsCompressed = true;

            Hierarchy h = LogManager.GetRepository() as Hierarchy;
                
            LimitedMemoryLogAppender memoryAppender = h.Root.GetAppender("MemoryAppender") as LimitedMemoryLogAppender;
            string[] filters = msg.Parms.GetStringArrayProperty(2);
            LoggingEvent[] evts = memoryAppender.GetEvents();
            List<string> logs = new List<string>();
            for (int i = 0; i < evts.Length; i++)
            {
                for (int x = 0; x < filters.Length; x++)
                {
                    if (evts[i].LoggerName == filters[x])
                    {
                        logs.Add(string.Format("[{2}]\t =>{0}\t: {1}", evts[i].TimeStamp.ToUniversalTime().ToString("hh:mm:ss tt"), evts[i].RenderedMessage, evts[i].Level.Name));
                    }
                }
            }

            r.ReplyPacket.Parms.SetProperty(1, MyServer.ServerUserID);
            r.ReplyPacket.Parms.SetProperty(2, logs.ToArray());
        }

        private void OnLogOverviewRequest(INetworkConnection con, Packet r)
        {
            Log1.Logger("Zeus").Debug("Log overview request from " + ServerUser.AccountName + ".");

            PacketGenericMessage msg = r as PacketGenericMessage;

            int id = msg.Parms.GetIntProperty(2).GetValueOrDefault(-1);
            r.ReplyPacket = CreateStandardReply(r, ReplyType.OK, "");
                
            log4net.ILog[] loggers = log4net.LogManager.GetCurrentLoggers();
            string[] LogChannels = new string[loggers.Length];

            for(int i = 0; i < LogChannels.Length; i++)
            {
                LogChannels[i] = loggers[i].Logger.Name;
            }

            r.ReplyPacket.Parms.SetProperty(1, MyServer.ServerUserID);
            r.ReplyPacket.Parms.SetProperty(2, LogChannels);
        }

        #endregion
       
#region Login

        /// <summary>
        /// Phase 1: Player requests login to the system.  This method attempts to authenticate the player (or create a new account, depending on method parms)
        /// </summary>
        public void OnLoginRequest(INetworkConnection con, Packet pMsg)
        {
            if (MyServer.RequireAuthentication)
            {
                DoDatabaseLogin(con, pMsg);
            }
            else
            {
                DoLocalLogin(con, pMsg);
            }
        }

        public bool IsLocalConnection()
        {
            try
            {
                bool isLocal = IsLocalIpAddress(((IPEndPoint)this.RemoteEndPoint).Address);
                Log1.Logger("Zeus.Inbound.Network").Debug("IsConnectionLocal(" + ((IPEndPoint)this.RemoteEndPoint).Address + ") = " + isLocal);
                return isLocal;
            }
            catch(Exception e)
            {
                return false;
            }
        }

        private void DoLocalLogin(INetworkConnection con, Packet pMsg)
        {
            bool authd = IsLocalConnection();
            try
            {
                PacketLoginRequest p = pMsg as PacketLoginRequest;
                ServerUser.AccountName = p.AccountName;
                Log1.Logger("Zeus").Info("Local user " + Environment.UserName + " is attempting login...");

                bool hasAccountAccess = authd;

                if (!hasAccountAccess)
                {
                    PacketLoginResult lrf = (PacketLoginResult)CreatePacket((int)PacketType.LoginResult, 0, true, true);
                    lrf.IsCritical = true; // this along with reply code failure forces D/C
                    lrf.ReplyCode = ReplyType.Failure;
                    lrf.ReplyMessage = string.Format(p.AccountName + " failed to authenticate local IP address.");
                    pMsg.ReplyPacket = lrf;

                    Log1.Logger("Zeus").Info(lrf.ReplyMessage);
                    return;
                }

                PacketLoginResult result = CreateLoginResultPacket();
                if (result.ReplyCode == ReplyType.OK)
                {
                    ServerUser.AuthTicket = Guid.NewGuid();
                    ServerUser.IsAuthenticated = true;
                    MembershipUser mu = Membership.GetUser(p.AccountName, true);
                    if (mu != null)
                    {
                        ServerUser.ID = (Guid)mu.ProviderUserKey;
                    }
                    else if (authd)
                    {
                        ServerUser.ID = Guid.NewGuid();
                    }

                    ConnectionManager.AuthorizeUser(ServerUser, false);
                    ServerUser.Profile.UserRoles = new string[] { "Administrator" };
                    result.Parms.SetProperty(-1, ServerUser.Profile.UserRoles);
                    result.Parms.SetProperty(-2, ServerUser.Profile.MaxCharacters);
                    LoggedInAndReady();
                }

                pMsg.ReplyPacket = result;                
                Log1.Logger("Zeus").Info("Zeus client *" + Environment.UserName + "* authentication: " + result.ReplyCode.ToString() + ". " + result.ReplyMessage);
            }
            catch (Exception e)
            {
                Log1.Logger("Zeus").Error("Exception thrown whilst player attempted login. " + e.Message, e);
                KillConnection("Error logging in.");
            }
        }

        /// <summary>
        /// User requests login to the system.  This method attempts to authenticate the player (or create a new account, depending on method parms)
        /// </summary>
        public void DoDatabaseLogin(INetworkConnection con, Packet pMsg)
        {
            PacketLoginRequest p = pMsg as PacketLoginRequest;
            ServerUser.AccountName = p.AccountName;
            Log1.Logger("Zeus").Info("User " + p.AccountName + " from " + RemoteIP + " is attempting login...");

            string msg = "";
            
#if DEBUG 
            DateTime start = DateTime.Now;
#endif      
            bool isLocal = IsLocalConnection();
            bool hasAccountAccess = isLocal || Membership.ValidateUser(p.AccountName, p.Password);
            
            
#if DEBUG
            DateTime end = DateTime.Now;
            TimeSpan len = end - start;
            Log1.Logger("Zeus").Debug("DB call to validate user took " + len.TotalSeconds.ToString() + " seconds.");
#endif

            if (!hasAccountAccess)
            {
                PacketLoginResult lrf = (PacketLoginResult)CreatePacket((int)PacketType.LoginResult, 0, true, true);
                lrf.IsCritical = true; // this along with reply code failure forces D/C
                lrf.ReplyCode = ReplyType.Failure;
                lrf.ReplyMessage = string.Format("Authentication Failed. No account matching these credentials was found. Goodbye.");
                pMsg.ReplyPacket = lrf;

                Log1.Logger("Zeus").Info(p.AccountName + ": " + lrf.ReplyMessage);
                return;
            }

            MembershipUser usr = Membership.GetUser(p.AccountName, true);
            if (usr == null && isLocal)
            {
                usr = new MembershipUser("CustomizedMembershipProvider", Environment.MachineName + "\\" + Environment.UserName, Guid.NewGuid(), "", "", "", true, false, DateTime.Now, DateTime.Now, DateTime.Now, DateTime.Now, DateTime.MinValue);
            }

            ServerUser.ID = (Guid)usr.ProviderUserKey;
            // Cache this user's profile data
            ServerUser.Profile.Load(MyServer.RequireAuthentication);

            if (isLocal)
            {
                ServerUser.Profile.UserRoles = new string[] { "Administrator" };
            }

            // check if suspension needs to be lifted
            DateTime suspensionRelease = ServerUser.Profile.AddedProperties.GetDateTimeProperty("SuspensionRelease").GetValueOrDefault(DateTime.MinValue);
            bool isSuspended = false;
            if (suspensionRelease != DateTime.MinValue)
            {
                isSuspended = true;
                // currently suspended.  need to lift suspension?
                if (suspensionRelease < DateTime.UtcNow)
                {
                    DB.Instance.User_Unsuspend(ServerUser.ID, "System", suspensionRelease, "Suspension expired.  Time served.", -1);
                    isSuspended = false;
                }
            }

            if (isSuspended && !ServerUser.Profile.IsUserInRole("Administrator"))
            {
                PacketLoginResult lrf = (PacketLoginResult)CreatePacket((int)PacketType.LoginResult, 0, true, true);
                lrf.IsCritical = true; // this along with reply code failure forces D/C
                lrf.ReplyCode = ReplyType.Failure;
                lrf.ReplyMessage = "[" + p.AccountName + "] is currently suspended until " + suspensionRelease.ToString("g") + " UTC. Access to Zeus is denied.";
                pMsg.ReplyPacket = lrf;

                Log1.Logger("LoginServer.Inbound.Login").Info(lrf.ReplyMessage);
                return;
            }

            bool accountIsActive = isLocal || ServerUser.Profile.IsUserInRole("ActiveCustomerService");
            if (!accountIsActive)
            {
                PacketLoginResult lrf = (PacketLoginResult)CreatePacket((int)PacketType.LoginResult, 0, true, true);
                lrf.IsCritical = true; // this along with reply code failure forces D/C
                lrf.ReplyCode = ReplyType.Failure;
                lrf.ReplyMessage = "Authentication Failed. This account does not have the proper credential to connect. Goodbye.";
                pMsg.ReplyPacket = lrf;

                Log1.Logger("Zeus.Inbound.Client.Login").Info(p.AccountName + ": " + lrf.ReplyMessage);
                return;
            }                                 

            PacketLoginResult result = CreateLoginResultPacket();
            if (result.ReplyCode == ReplyType.OK)
            {
                ServerUser.AuthTicket = Guid.NewGuid();
                ServerUser.IsAuthenticated = true;
                ServerUser.ID = (Guid)usr.ProviderUserKey;

                ConnectionManager.AuthorizeUser(ServerUser, false);                
                result.Parms.SetProperty(-1, ServerUser.Profile.UserRoles);
                result.Parms.SetProperty(-2, ServerUser.Profile.MaxCharacters);
                result.Parms.SetProperty((int)PropertyID.Name, MyServer.ServerUserID);
                LoggedInAndReady();
            }
            pMsg.ReplyPacket = result;

            Log1.Logger("Zeus").Info("Zeus client *" + ServerUser.AccountName + "* authentication: " + result.ReplyCode.ToString() + ". " + result.ReplyMessage);
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
                string serverInfo = g.ID+ "," + g.HasLiveOutboundServerConnections;
                lr.ReplyMessage += "|" + serverInfo;
            }

            lr.ReplyMessage = lr.ReplyMessage.Trim('|');            

            if (!haveAnyOnline)
            {
                lr.ReplyMessage = "No Wisp servers are currently online on this machine.";
            }

            Log1.Logger("Zeus.Inbound.Client.Login").Debug("Sending client " + ServerUser.AccountName + " Wisp server info: " + lr.ReplyMessage);
            return lr;
        }
       
#endregion

#region Server Commands
        
        private void OnServerCommandListRequest(INetworkConnection con, Packet r)
        {
            Log1.Logger("Zeus").Debug("Server command listing request from " + ServerUser.AccountName + ".");

            PacketGenericMessage msg = r as PacketGenericMessage;
            string commandGroup = msg.Parms.GetStringProperty(2).Trim();
            r.ReplyPacket = CreateStandardReply(r, ReplyType.OK, "");
            r.ReplyPacket.Parms.SetProperty(1, MyServer.ServerUserID);
            List<CommandData> cmds = CommandManager.GetCommands(commandGroup);
            r.ReplyPacket.Parms.SetProperty(2, cmds.ToArray());
        }
        
        private void OnServerCommandExecuteRequest(INetworkConnection con, Packet r)
        {
            Log1.Logger("Zeus").Debug("Server command execution request from " + ServerUser.AccountName + ".");

            PacketGenericMessage msg = r as PacketGenericMessage;
            string commandName = msg.Parms.GetStringProperty(2).Trim();
            string[] parms = msg.Parms.GetStringArrayProperty(3);

            // if this message is coming from another server, we should have the roles in the message, if 
            // not, then its coming from a user and we should have the roles in the user profile
            string[] roles = ServerUser.IsAuthorizedClusterServer ? msg.Parms.GetStringArrayProperty("Roles") : ServerUser.Profile.UserRoles;

            r.ReplyPacket = CreateStandardReply(r, ReplyType.OK, "");
            r.ReplyPacket.Parms.SetProperty(1, MyServer.ServerUserID);

            string result = CommandManager.ExecuteCommand(ServerUser.AccountName, roles, commandName, parms);
            r.ReplyPacket.Parms.SetProperty(2, commandName);
            r.ReplyPacket.Parms.SetProperty(3, parms);
            r.ReplyPacket.Parms.SetProperty(4, result);
        }     

#endregion

        protected override void OnSocketKilled(string msg)
        {
            base.OnSocketKilled(msg);
            Log1.Logger("Zeus").Info("Connection to " + ServerUser.AccountName + " terminated.(" + msg + ") ");
        }
       
        private static IPAddress[] localIPs;
        private static bool IsLocalIpAddress(IPAddress addy)
        {
            try
            { 
                // get local IP addresses
                if (localIPs == null)
                {
                    localIPs = Dns.GetHostAddresses(Dns.GetHostName());
                }

                // test if any host IP equals to any local IP or to localhost
             
                    // is localhost
                if (IPAddress.IsLoopback(addy) || addy.Equals(IPAddress.IPv6Loopback) || addy.Equals(IPAddress.Loopback) || addy.ToString().EndsWith("127.0.0.1")) return true;
                    
                    // is local address
                foreach (IPAddress localIP in localIPs)
                {
                    if (addy.Equals(localIP)) return true;
                }
             
            }
            catch { }
            return false;
        }

        protected virtual void LoggedInAndReady()
        {
            RegisterPacketHandler((int)PacketType.PacketGenericMessage, (int)GenericZeusPacketType.RequestConfigListing, OnConfigRequest);
            RegisterPacketHandler((int)PacketType.PacketGenericMessage, (int)GenericZeusPacketType.SaveConfigListing, OnConfigSaveRequest);
            RegisterPacketHandler((int)PacketType.PacketGenericMessage, (int)GenericZeusPacketType.RequestCommandOverview, OnServerCommandListRequest);
            RegisterPacketHandler((int)PacketType.PacketGenericMessage, (int)GenericZeusPacketType.ExecuteCommand, OnServerCommandExecuteRequest);
            RegisterPacketHandler((int)PacketType.PacketGenericMessage, (int)GenericZeusPacketType.RequestLogOverview, OnLogOverviewRequest);
            RegisterPacketHandler((int)PacketType.PacketGenericMessage, (int)GenericZeusPacketType.RequestLogs, OnLogRequest);
        }

    }
}
