using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;

namespace Shared
{
    /// <summary>
    /// Represents one connection from another server in the cluster
    /// This class can send and receive player handoff requests to/from the remote server.  Override OnPlayerConnectionRequest
    /// if you want to control what players are allowed to be transferred.  Override OnPlayerHandoffResponseReceived to 
    /// actually do whatever needs to be done in your implementation to initiate the player transfer (i.e. send a redirect
    /// message to the client or whatever else is appropriate in your circumstance).
    /// </summary>
    public class InboundServerConnection : InboundConnection
    {
        public InboundServerConnection(Socket s, ServerBase server, bool isBlocking)
            : base(s, server, isBlocking)
        {           
        }

        private static bool m_IsInitialized = false;
        protected override void OnInitialize()
        {
            base.OnInitialize();
            if (!m_IsInitialized)
            {
                RegisterPacketCreationDelegate((int)ServerPacketType.PacketPlayerAuthorizedForTransfer, delegate { return new PacketPlayerAuthorizedForTransfer(); });
                RegisterPacketCreationDelegate((int)ServerPacketType.Relay, delegate { return new PacketRelay(); });
                RegisterPacketCreationDelegate((int)ServerPacketType.RequestPlayerHandoff, delegate { return new PacketRelayPlayerHandoffRequest(); });
                m_IsInitialized = true;
            }

            RegisterPacketHandler((int)ServerPacketType.PacketPlayerAuthorizedForTransfer, OnPlayerHandoffResult);
            RegisterPacketHandler((int)PacketType.LoginRequest, OnClusterServerLoginRequest);
            RegisterPacketHandler((int)ServerPacketType.Relay, OnPacketRelayRequest);
        }

        private void OnPacketRelayRequest(INetworkConnection con, Packet gmsg)
        {
            PacketRelay relay = (PacketRelay)gmsg;
            INetworkConnection userCon = ConnectionManager.GetUserConnection(relay.To);
            if (userCon == null)
            {
                // user not attached. sorry it didn't work out.
                return;
            }

            userCon.Send(relay.Message, relay.Flags);
        }

        /// <summary>
        /// Override to handle player transfer/handoff requests.  If this method is not overridden, ALL players will be able to connect.  If you wish
        /// to deny player login, return Guid.Empty, otherwise generate a new authentication ticket GUID and optionally track it at your leisure.
        /// The default implementation does not check to see if there are any outbound server nodes attached to this server.  In other words, 
        /// if this is a central server, it will allow the transfer even if there are no game servers nodes attached, which may be a problem
        /// in some implementations.  Override this method and determine if you want to allow the transfer based on whatever criteria are
        /// important in your implementation.
        /// </summary>
        /// <param name="accountName">the account name</param>
        /// <param name="player">the user that represents the account wanting to log in</param>
        /// <returns>The authentication ticket. Return Guid.Empty to deny login, otherwise return a new GUID</returns>
        public virtual Guid OnPlayerConnectionRequest(ServerUser player, ref string msg)
        {            
            return Guid.NewGuid();
        }

        /// <summary>
        /// Gets called when the parent server wishes to handoff a player off
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
            
            if (msg.Character != null)
            {
                msg.Character.OwningAccount = su;
                su.CurrentCharacter = msg.Character;                
                su.CurrentCharacter.TargetResource = msg.TargetResource;
            }

            string rmsg = "";
            su.AuthTicket = OnPlayerConnectionRequest(su, ref rmsg);

            bool allowed = su.AuthTicket != Guid.Empty;

            if (allowed)
            {                
                ConnectionManager.AuthorizeUser(su, false);
                if (msg.Character != null)
                {
                    CharacterCache.CacheCharacter(su.CurrentCharacter, ServerUser.AccountName);
                }
            }

            PacketPlayerAuthorizedForTransfer p = (PacketPlayerAuthorizedForTransfer)CreatePacket((int)ServerPacketType.PacketPlayerAuthorizedForTransfer, 0, true, true);
            p.ReplyCode = allowed ? ReplyType.OK : ReplyType.Failure;
            p.Profile = msg.Profile;
            p.AccountName = msg.AccountName;
            p.AuthTicket = su.AuthTicket;
            p.ReplyMessage = allowed ? "Welcome to " + MyServer.ServerName + ", " + msg.AccountName : "Server is currently not accepting logins.  Try again a little later. " + rmsg;
            p.Player = msg.Player;
            p.TargetResource = msg.TargetResource;

            msg.ReplyPacket = p; // reply will be sent by NetworkConnection.OnAfterPacketProcessed            
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

        private void OnCharacterHandoffComplete(INetworkConnection con, Packet gmsg)
        {
            PacketGenericMessage pck = gmsg as PacketGenericMessage;
            ServerCharacterInfo ci = pck.Parms.GetComponentProperty((int)PropertyID.CharacterInfo) as ServerCharacterInfo;
            Guid owner = pck.Parms.GetGuidProperty((int)PropertyID.Owner);
            Log1.Logger("Server.Inbound.Network").Info("Character [" + ci.CharacterName + "|#" + ci.ID.ToString() + "] completed transfer to " + ((InboundServerConnection)con).ServerUser.AccountName);
            OnCharacterHandoffComplete(con, ci, owner);
        }

        /// <summary>
        /// Gets called when the login attempt from the cluster server has resolved.  Return false from this method to prevent login.
        /// Be sure to call base.OnClusterServerLoginResolved to register necessary packet handler
        /// </summary>
        protected virtual bool OnClusterServerLoginResolved(PacketLoginRequest login, bool authenticationResult)
        {
            if (authenticationResult)
            {
                RegisterPacketHandler((int)ServerPacketType.RequestPlayerHandoff, OnPlayerHandoffRequest);
                RegisterPacketHandler((int)PacketType.PacketGenericMessage, (int)GenericMessageType.CharacterTransferComplete, OnCharacterHandoffComplete);
                RegisterPacketHandler((int)PacketType.PacketGenericMessage, (int)GenericMessageType.CharacterDisconnected, OnRemoteCharacterDisconnected);
            }
            return authenticationResult;
        }

        protected virtual void OnRemoteCharacterDisconnected(INetworkConnection con, int characterId, string transferTarget)
        {
        }

        private void OnRemoteCharacterDisconnected(INetworkConnection con, Packet gmsg)
        {
            PacketGenericMessage msg = gmsg as PacketGenericMessage;
            int id = msg.Parms.GetIntProperty((int)PropertyID.CharacterId).GetValueOrDefault(-1);
            string transferTarget = msg.Parms.GetStringProperty((int)PropertyID.Name);
            Log1.Logger("Server.Inbound.Network").Info("Character id " + id + " disconnected from remote server. Transfer target is " + (transferTarget.Length > 0 ? transferTarget : "empty!") + ".");
            OnRemoteCharacterDisconnected(con, id, transferTarget);
        }

        /// <summary>
        /// Gets called when a parent server is requesting to be connected with this server.
        /// There is no account in the accounts DB for the parent servers... there is only a shared secret which is shared among all servers in the clusters.
        /// The shared secret is defined in the App.Config.
        /// </summary>
        private void OnClusterServerLoginRequest(INetworkConnection con, Packet msg)
        {
            PacketLoginRequest p = msg as PacketLoginRequest;
            m_ServerUser.AccountName = p.AccountName;

            // send login result
            PacketLoginResult lr = (PacketLoginResult)CreatePacket((int)PacketType.LoginResult, 0, true, true);
            //lr.ReplyMessage = ServerBase.ServerName;
            lr.Parms.SetProperty(1, MyServer.ServerName);
            lr.ReplyCode = (p.Password.ToUpper() == SharedSecretWithClusterServers.ToString().ToUpper()) ? ReplyType.OK : ReplyType.Failure;
            if (lr.ReplyCode == ReplyType.Failure)
            {
                lr.ReplyMessage = "Bad credentials.";
            }
            
            if (!OnClusterServerLoginResolved(p, lr.ReplyCode == ReplyType.OK))
            {
                lr.ReplyCode = ReplyType.Failure;
            }

            if (lr.ReplyCode == ReplyType.OK)
            {
                lr.Parms.SetProperty((int)PropertyID.Name, MyServer.ServerUserID);
                m_ServerUser.AccountName = p.AccountName;
                m_ServerUser.IsAuthorizedClusterServer = true;
                m_ServerUser.RenewAuthorizationTicket(); // make sure we dont time out
            }

            msg.ReplyPacket = lr;

            if (lr.ReplyCode != ReplyType.OK )
            {
                lr.IsCritical = true;
                Log1.Logger("Server.Inbound.Login").Info("Failed authentication for " + p.AccountName + ", using password *" + p.Password + "* . Killing connection.");
                return;
            }

            RegisterPacketHandler((int)PacketType.Null, OnPing);

            ConnectionManager.AddParentConnection(this, m_ServerUser);
            OnParentConnectionSet();
            OnConnectionReady();
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
            p.SharedSecret = MyServer.SharedClusterServerSecret;
            p.AccountName = accountName;
            p.TargetResource = targetResource;
            p.Profile = profile;
            p.OwningServer = owningServer;
            p.Character = characterToTransfer;
            characterToTransfer.TargetResource = targetResource;


            Log1.Logger("Server.Inbound.Network").Info("Requesting authenticated client *" + p.AccountName + "* to be handed off to game server " + ServerUser.AccountName + ".");
            Send(p);
        }

        /// <summary>
        /// Gets called when we get a response to RequestPlayerHandoff()
        /// </summary>
        protected virtual void OnPlayerHandoffResponseReceived(PacketPlayerAuthorizedForTransfer msg, ServerUser user)
        {            
            if (user != null && RemoteEndPoint != null)
            {
                string targetServer = "";
                IPEndPoint ep = RemoteEndPoint as IPEndPoint;

                if (msg.ReplyCode == ReplyType.OK)
                {
                    targetServer = ServerUser.AccountName == null ? "Unknown Server User ID" : ServerUser.AccountName;
                }
                
                user.TransferToServerAssisted(
                    ep.Address.ToString(),
                    ep.Port,
                    msg.AuthTicket,
                    msg.TargetResource,
                    targetServer,
                    targetServer,
                    msg.ReplyMessage,
                    msg.ReplyCode);             
            }
        }

        /// <summary>
        /// Gets called when a game sub-server sends us a reply to our request to have a player play on that server.  This method forwards that reply to the player.
        /// </summary>
        private void OnPlayerHandoffResult(INetworkConnection sender, Packet msg)
        {
            PacketPlayerAuthorizedForTransfer p = msg as PacketPlayerAuthorizedForTransfer;
            ServerUser playerCon = ConnectionManager.GetAuthorizedUser(p.AccountName, MyServer, PacketLoginRequest.ConnectionType.AssistedTransfer);
            if (playerCon != null)
            {
                Log1.Logger("Server.Inbound.Network").Info("Remote server [" + ServerUser.AccountName + " replied to transfer request of Account [" + p.AccountName + "] : [" + p.ReplyCode.ToString() + "]. Forwarding result to player.");
            }
            OnPlayerHandoffResponseReceived(p, playerCon);
        }

        protected virtual void OnPing(INetworkConnection con, Packet ping)
        {
           // Log1.Logger("Server.Inbound.Network").Debug("Sending heartbeat to " + ((InboundConnection)con).ServerUser.AccountName + ".");
            PacketServerUpdate pong = (PacketServerUpdate)CreatePacket((int)ServerPacketType.ServerStatusUpdate, 0, false, false);
            pong.ServerName = MyServer.ServerName;
            pong.UserID = MyServer.ServerUserID;
            pong.MaxPlayers = MyServer.MaxInboundConnections;
            pong.CurrentPlayers = ConnectionManager.PaddedConnectionCount;
            ping.ReplyPacket = pong;            
        }     

        /// <summary>
        /// Gets called when we are ready to communicate with the server, i.e. we can start sending packets
        /// </summary>
        protected virtual void OnConnectionReady()
        {
        }

    }
}
