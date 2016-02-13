using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace Shared
{
    /// <summary>
    /// A connection initiated by a server in the cluster (the "parent"), to another server in the cluster.
    /// This class can send and receive player handoff requests to/from the remote server.  Override OnPlayerConnectionRequest
    /// if you want to control what players are allowed to be transferred.  Override OnPlayerHandoffResponseReceived to 
    /// actually do whatever needs to be done in your implementation to initiate the player transfer (i.e. send a redirect
    /// message to the client or whatever else is appropriate in your circumstance).
    /// </summary>
    public class OutboundServerConnection : ClientConnection
    {
        public delegate void DisconnectedDelegate(OutboundServerConnection con, string msg);
        
        /// <summary>
        /// Fires when this outgoing connection disconnects for any reason
        /// </summary>
        public event DisconnectedDelegate Disconnected;

        private string m_Name;
        /// <summary>
        /// The target server's name.  Should only be used for display purposes.  User ServerUserID for all identification.
        /// </summary>
        public string Name
        {
            get { return m_Name; }
            set 
            {
                m_Name = value;
            }
        }

        /// <summary>
        /// The internal user server ID for this server
        /// </summary>
        public string ServerUserID { get; set; }

        /// <summary>
        /// Checks to see if the connection was active at some point and then was terminated.  If true,
        /// the connection should not be used again, but should isntead be recycled.
        /// </summary>
        public bool Disonnected { get; set; }

        /// <summary>
        /// Reference to the server object tht this connection was initiated from
        /// </summary>
        public ServerBase Server { get; set; }

        /// <summary>
        /// The server address of the server this connection represents
        /// </summary>
        public string ReportedIP { get; set; }

        public OutboundServerConnection(string name, ServerBase server, string reportedIP, bool isBlocking)
            : base(isBlocking)
        {
            Name = name;
            LoggedIn = false;
            Disonnected = false;
            Server = server;
            ReportedIP = reportedIP;

            RegisterPacketHandler((int)ServerPacketType.ServerStatusUpdate, OnServerPong);
        }

        static OutboundServerConnection()
        {
        }

        /// <summary>
        /// Don't use this, unless you're going to set this.Name manually. In fact, don't use it at all.
        /// </summary>
        private OutboundServerConnection(bool isBlocking) : base(isBlocking)
        {
            throw new NotSupportedException();
        }

        private static bool m_IsInitialized = false;
        protected override void OnInitialize()
        {
            base.OnInitialize();
            if (!m_IsInitialized)
            {
                RegisterPacketCreationDelegate((int)ServerPacketType.PacketPlayerAuthorizedForTransfer, delegate { return new PacketPlayerAuthorizedForTransfer(); });
                RegisterPacketCreationDelegate((int)ServerPacketType.ServerStatusUpdate, delegate { return new PacketServerUpdate(); });
                RegisterPacketCreationDelegate((int)ServerPacketType.RequestPlayerHandoff, delegate { return new PacketRelayPlayerHandoffRequest(); });
                RegisterPacketCreationDelegate((int)ServerPacketType.Relay, delegate { return new PacketRelay(); });

                m_IsInitialized = true;
            }

            RegisterPacketHandler((int)ServerPacketType.PacketPlayerAuthorizedForTransfer, OnPlayerHandoffResult);
            RegisterPacketHandler((int)ServerPacketType.RequestPlayerHandoff, OnPlayerHandoffRequest);
            RegisterPacketHandler((int)PacketType.PacketGenericMessage, (int)GenericMessageType.CharacterTransferComplete, OnCharacterHandoffComplete);
            RegisterPacketHandler((int)PacketType.PacketGenericMessage, (int)GenericMessageType.CharacterDisconnected, OnRemoteCharacterDisconnected);
            RegisterPacketHandler((int)ServerPacketType.Relay, OnPacketRelayRequest);
        }

        private void OnPacketRelayRequest(INetworkConnection con, Packet gmsg)
        {
            PacketRelay relay = (PacketRelay)gmsg;
            INetworkConnection userCon = ConnectionManager.GetUserConnection(relay.To);
            if (userCon == null)
            {
                // target user not attached.  sorry it didn't work out.
                return;
            }

            userCon.Send(relay.Message, relay.Flags);
        }

        protected virtual void OnRemoteCharacterDisconnected(INetworkConnection con, int characterId, string transferTarget)
        {
        }

        private void OnRemoteCharacterDisconnected(INetworkConnection con, Packet gmsg)
        {
            PacketGenericMessage msg = gmsg as PacketGenericMessage;
            int id = msg.Parms.GetIntProperty((int)PropertyID.CharacterId).GetValueOrDefault(-1);
            string transferTarget = msg.Parms.GetStringProperty((int)PropertyID.Name);
            Log1.Logger("Server.Outbound.Network").Info("Character id " + id + " disconnected from remote server.");
            OnRemoteCharacterDisconnected(con, id, transferTarget);
        }

        /// <summary>
        /// Gets called when a character/user has successfully arrived at a remote server
        /// </summary>
        /// <param name="con">the remote server connection sending the notification</param>
        /// <param name="character">the character that was transferred</param>
        /// <param name="owner">The owner's account ID</param>
        protected virtual void OnCharacterHandoffComplete(INetworkConnection con, ServerCharacterInfo character, Guid owner)
        {
        }

        private void OnCharacterHandoffComplete(INetworkConnection con, Packet msg)
        {
            PacketGenericMessage pck = msg as PacketGenericMessage;
            ServerCharacterInfo ci = pck.Parms.GetComponentProperty((int)PropertyID.CharacterInfo) as ServerCharacterInfo;
            Guid owner = pck.Parms.GetGuidProperty((int)PropertyID.Owner);
            Log1.Logger("Server.Outbound.Network").Info("Character [" + ci.CharacterName + "|#" + ci.ID.ToString() + "] completed transfer to " + ((OutboundServerConnection)con).Name);
            OnCharacterHandoffComplete(con, ci, owner);
        }

        /// <summary>
        /// Requests that a player be connected to the cluster server
        /// </summary>
        /// <param name="userId">the id of the account to be transferred</param>
        /// <param name="accountName">the name of the account to be transferred</param>
        /// <param name="remotePlayerEndpoint">the remote endpoint of the transferee</param>
        /// <param name="targetResource">the target content resource the player wants, or Guid.Empy if none</param>
        /// <param name="accountProperties">the account properties of the account to transfer</param>
        /// <param name="owningServer">the server in the cluster that owns the server.  See ServerUser.Owner</param>
        public void RequestPlayerHandoff(Guid userId, string accountName, Guid targetResource, AccountProfile profile, ServerCharacterInfo characterToTransfer, string owningServer)
        {
            // request auth ticket from game server
            PacketRelayPlayerHandoffRequest p = (PacketRelayPlayerHandoffRequest)CreatePacket((int)ServerPacketType.RequestPlayerHandoff, 0, false, false);
            p.NeedsReply = true;
            p.Player = userId;
            p.SharedSecret = Server.SharedClusterServerSecret;
            p.AccountName = accountName;
            p.TargetResource = targetResource;
            p.Profile = profile;
            p.OwningServer = owningServer;
            p.Character = characterToTransfer;

            Log1.Logger("Server.Outbound.Network").Info("Requesting authenticated client *" + p.AccountName + "* to be handed off to game server " + Name + ".");
            Send(p);
        }

        /// <summary>
        /// Gets called when a game sub-server sends us a reply to our request to have a player play on that server.  This method forwards that reply to the player.
        /// </summary>
        private void OnPlayerHandoffResult(INetworkConnection sender, Packet msg)
        {
            PacketPlayerAuthorizedForTransfer p = msg as PacketPlayerAuthorizedForTransfer;
            ServerUser playerCon = ConnectionManager.GetAuthorizedUser(p.AccountName, Server, PacketLoginRequest.ConnectionType.AssistedTransfer);
            if (playerCon != null)
            {
                Log1.Logger("Server.Outbound.Network").Info("Account [" + playerCon.AccountName + "] transfer request to " + Name + " reply: [" + p.ReplyCode.ToString() + "]. Forwarding result to player.");
            }
            OnPlayerHandoffResponseReceived(p, playerCon);
        }

        /// <summary>
        /// Gets called when we get a response to RequestPlayerHandoff().  Base method forwards a PacketGameServerAccessGranted packet to the player.
        /// Don't call the base method if you don't want that packet sent.
        /// </summary>
        protected virtual void OnPlayerHandoffResponseReceived(PacketPlayerAuthorizedForTransfer msg, ServerUser user)
        {
            if (user != null && RemoteEndPoint != null)
            {
                string targetServer = "";
                IPEndPoint ep = RemoteEndPoint as IPEndPoint;

                if (msg.ReplyCode == ReplyType.OK)
                {
                    targetServer = ServerUserID == null ? "Unknown Server User ID" : ServerUserID;
                }

                user.TransferToServerAssisted(
                    ep.Address.ToString(),
                    ep.Port,
                    msg.AuthTicket,
                    msg.TargetResource,
                    Name == null ? "" : Name,
                    targetServer,
                    msg.ReplyMessage,
                    msg.ReplyCode);
            }
        }

        /// <summary>
        /// Fires when a child server replies with a heartbeat pong.  Also updates the GSI object with things like current number of users
        /// </summary>
        protected virtual void OnServerPong(INetworkConnection con, Packet msg)
        {
            PacketServerUpdate pong = msg as PacketServerUpdate;
            IPEndPoint ipe = (IPEndPoint)((OutboundServerConnection)con).RemoteEndPoint;
            GameServerInfo<OutboundServerConnection> gi = Server.GetGSIByIPAndPort(ipe.Address.ToString(), ipe.Port);
            if (gi == null)
            {
                return;
            }
            //Log1.Logger("Server.Outbound.Network").Debug("Got server heartbeat from " + pong.UserID + "/" + pong.ServerName);
            gi.UserID = ServerUserID = pong.UserID;
            gi.Name = gi.Connection.Name = pong.ServerName;
            gi.CurUsers = pong.CurrentPlayers;
            gi.MaxUsers = pong.MaxPlayers;
            gi.LastUpdate = DateTime.UtcNow;
            gi.IsOnline = true;
            Server.UpdateOutboundServerAvailability(gi.ServerGroup);

            if (ServerUserID == null || ServerUserID.Length < 1)
            {
                Log1.Logger("Server.Outbound.Network").Error("Outbound server " + pong.ServerName + " did not indicate their username to us.  Was one set in that server's App.Config?.  Disconnecting from that server. We need that ID.");
                gi.Connection.KillConnection("ServerUserID not set on remote server.");
            }
        }

        /// <summary>
        /// The target child server requires that we log in with our shared secret (App.Config) before it will
        /// entertain any other requests from us.  This method fires when the target server
        /// resolves the login request.
        /// </summary>
        protected override void OnServerLoginResponse(PacketLoginResult result)
        {
            base.OnServerLoginResponse(result);
            if (result.ReplyCode != ReplyType.OK)
            {
                Log1.Logger("Server").Error("Unable to login to target server [" + Name + "]. [" + result.ReplyMessage + "].");
                return;
            }

            string ip = "";
            ip = ((IPEndPoint)MyTCPSocket.RemoteEndPoint).Address.ToString();
            int port = -1;
            port = ((IPEndPoint)MyTCPSocket.RemoteEndPoint).Port;
            string serverName = result.Parms.GetStringProperty("ServerName");
            if (result.ReplyCode == ReplyType.OK)
            {
                ServerUserID = result.Parms.GetStringProperty((int)PropertyID.Name);
            }
            Server.UpdateGSIIP(ReportedIP, port, ip, serverName);

            PacketNull ping = CreatePacket((int)PacketType.Null, 0, false, false) as PacketNull;
            ping.NeedsReply = true;
            Send(ping);
        }

        /// <summary>
        /// Override to handle player transfer/handoff requests.  If this method is not overridden, ALL players will be able to connect.  If you wish
        /// to deny player login, return Guid.Empty, otherwise generate a new authentication ticket GUID and optionally track it at your leisure.
        /// </summary>
        /// <param name="player">the user that represents the account wanting to log in</param>
        /// <returns>The authentication ticket. Return Guid.Empty to deny login, otherwise return a new GUID</returns>
        public virtual Guid OnPlayerConnectionRequest(ServerUser player, ref string msg)
        {
            return Guid.NewGuid();
        }

        /// <summary>
        /// Gets called when the parent server wishes to handoff a player to one of our sub-servers
        /// </summary>
        /// <param name="msg">request details</param>
        private void OnPlayerHandoffRequest(INetworkConnection con, Packet pck)
        {
            PacketRelayPlayerHandoffRequest msg = pck as PacketRelayPlayerHandoffRequest;

            // Create an auth ticket for the player, if we want to allow them to connect.
            ServerUser su = new ServerUser();
            su.OwningServer = msg.OwningServer;
            su.AuthTicket = Guid.Empty;
            su.ID = msg.Player;
            su.AccountName = msg.AccountName;
            su.Profile = msg.Profile;

            msg.Character.OwningAccount = su;
            su.CurrentCharacter = msg.Character;
            su.CurrentCharacter.TargetResource = msg.TargetResource;

            string rmsg = "";
            su.AuthTicket = OnPlayerConnectionRequest(su, ref rmsg);

            bool allowed = su.AuthTicket != Guid.Empty;

            if (allowed)
            {
                ConnectionManager.AuthorizeUser(su);
                if (msg.Character != null)
                {
                    CharacterCache.CacheCharacter(su.CurrentCharacter, ServerUserID);
                }
            }

            PacketPlayerAuthorizedForTransfer p = (PacketPlayerAuthorizedForTransfer)CreatePacket((int)ServerPacketType.PacketPlayerAuthorizedForTransfer, 0, true, true);
            p.ReplyCode = allowed ? ReplyType.OK : ReplyType.Failure;
            p.Profile = msg.Profile;
            p.AccountName = msg.AccountName;
            p.AuthTicket = su.AuthTicket;
            p.ReplyMessage = allowed ? "Welcome to " + Server.ServerName + ", " + msg.AccountName : "Server is currently not accepting logins.  Try again a little later. " + rmsg;
            p.Player = msg.Player;
            p.TargetResource = msg.TargetResource;

            msg.ReplyPacket = p; // reply will be sent by NetworkConnection.OnAfterPacketProcessed            
        }

        /// <summary>
        /// If the socket is disconnected for any reason
        /// </summary>
        /// <param name="msg"></param>
        protected override void OnSocketKilled(string msg)
        {
            LoggedIn = false;
            Disonnected = true;
            base.OnSocketKilled(msg);
            string name = "";
            if (this.AccountName != null)
            {
                name = AccountName;
                if (name == null)
                {
                    name = "";
                }
            }
            if (Disconnected != null)
            {
                Disconnected(this, msg);
            }
        }
        
    }
}
