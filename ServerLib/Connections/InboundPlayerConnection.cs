using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace Shared
{
    /// <summary>
    /// Handles a single player connection to a game server.  Manages player login logic.
    /// </summary>
    public class InboundPlayerConnection : InboundConnection
    {
        public InboundPlayerConnection(Socket s, ServerBase server, bool isBlocking)
            : base(s, server, isBlocking)
        {
        }

        private static bool m_IsInitialized = false;
        protected override void OnInitialize()
        {
            base.OnInitialize();
            if (!m_IsInitialized)
            {              
                m_IsInitialized = true;
            }

            RegisterPacketHandler((int)PacketType.LoginRequest, OnPlayerLoginRequest);
        }

        /// <summary>
        /// Gets called when the player login/transfer request has been resolved - i.e. the player connected and gave us login credentials.  Return false to prevent login. Don't forget to call base.OnPlayerLoginResolved 
        /// if you override this method so that all necessary packet handlers can be registered.
        /// </summary>
        protected virtual bool OnPlayerLoginResolved(PacketLoginRequest login, bool result, ref string msg)
        {
            if (result)
            {
            }
            return result;
        }

        /// <summary>
        /// Gets called when a player sends a login request ticket.
        /// </summary>
        private void OnPlayerLoginRequest(INetworkConnection con, Packet msg)
        {
            try
            {
                string loginMsg = "";
                PacketLoginRequest packetLoginRequest = msg as PacketLoginRequest;
                msg.NeedsReply = false; // handle sending ourselves because we dont want OnConnectionReady to fire before the player gets the login result.

                ServerUser su = null;
                su = ConnectionManager.GetAuthorizedUser(packetLoginRequest.AccountName, MyServer, packetLoginRequest.LoginConnectionType); ;

                PacketLoginResult lrf = (PacketLoginResult)CreatePacket((int)PacketType.LoginResult, 0, true, true);              
                if (su == null || su.AuthTicket.ToString() != packetLoginRequest.Password)
                {
                    OnPlayerLoginResolved(packetLoginRequest, false, ref loginMsg);
                    lrf.IsCritical = true;
                    lrf.ReplyCode = ReplyType.Failure;
                    lrf.ReplyMessage = "Account credentials refused by server! " + loginMsg;
                    msg.ReplyPacket = lrf;
                    return;
                }

                lrf.ReplyCode = ReplyType.OK;
                lrf.ReplyMessage = su.AccountName + ", ready to play!";
                lrf.Parms.SetProperty("ServerName", 1, MyServer.ServerName);
                msg.ReplyPacket = lrf;

                ServerUser.CurrentCharacter = su.CurrentCharacter;
                su.MyConnection = this;
                ServerUser.MyConnection = null;
                this.ServerUser = su;

                if (!OnPlayerLoginResolved(packetLoginRequest, true, ref loginMsg))
                {
                    lrf.ReplyCode = ReplyType.Failure;
                    lrf.IsCritical = true;
                    lrf.ReplyMessage = "Login Failed! " + loginMsg;
                    return;
                }

                Log1.Logger("Server.Inbound.Login").Info(packetLoginRequest.AccountName + ": " + lrf.ReplyMessage);

                lrf.Parms.SetProperty(-1, ServerUser.Profile.UserRoles);
                lrf.Parms.SetProperty(-2, ServerUser.Profile.MaxCharacters);
                msg.NeedsReply = false; // don't want to fire OnConnectionReady before the client gets the login reply
                Send(lrf);              // " "  

                OnConnectionReady();
            }
            catch (Exception e)
            {
                Log1.Logger("Server.Inbound.Login").Error("InboundPlayerConnection::OnPlayerLoginRequest failed. ", e);
                PacketLoginResult lrf = (PacketLoginResult)CreatePacket((int)PacketType.LoginResult, 0, true, true);
                lrf.ReplyCode = ReplyType.Failure;
                lrf.IsCritical = true;
                lrf.ReplyMessage = "Login Failed! " + e.Message;
                msg.ReplyPacket = lrf;
            }
        }

        private INetworkConnection GetOwningServerConnection()
        {
            INetworkConnection con = null;
            if ( ServerUser != null &&  ServerUser.OwningServer.Length > 0)
            {
                if (ServerUser.OwningServer == MyServer.ServerUserID)
                {
                    // don't need to let ourselves know.
                    return con;
                }

                con = ConnectionManager.GetParentConnection(ServerUser.OwningServer);
                if (con == null)
                {
                    GameServerInfo<OutboundServerConnection> ocon = MyServer.GetOutboundServerByServerUserID(ServerUser.OwningServer);
                    if (ocon == null)
                    {
                        Log1.Logger("Server.Inbound.Network").Error("Player was transferred by " + ServerUser.OwningServer + ", but that server couldn't be found.");
                        return null;
                    }
                    con = ocon.Connection;
                }
            }
            return con;
        }

        /// <summary>
        /// Gets called when we are ready to communicate with the player, i.e. we can start sending packets
        /// </summary>
        protected virtual void OnConnectionReady()
        {
            INetworkConnection con = GetOwningServerConnection();
            
            if (con != null && con.IsAlive && ServerUser.CurrentCharacter != null)
            {
                PropertyBag bag = new PropertyBag();
                bag.SetProperty((int)PropertyID.CharacterInfo, ServerUser.CurrentCharacter as IComponent);
                bag.SetProperty((int)PropertyID.Owner, ServerUser.ID);
                con.SendGenericMessage((int)GenericMessageType.CharacterTransferComplete, bag, false);
            }
        }

        protected override void OnSocketKilled(string msg)
        {
            INetworkConnection con = GetOwningServerConnection();

            if (con != null && con.IsAlive && ServerUser.CurrentCharacter != null)
            {
                PropertyBag bag = new PropertyBag();
                bag.SetProperty((int)PropertyID.CharacterId, ServerUser.CurrentCharacter.CharacterInfo.ID);
                if (ServerUser.TransferTarget != null)
                {
                    bag.SetProperty((int)PropertyID.Name, ServerUser.TransferTarget);
                }
                con.SendGenericMessage((int)GenericMessageType.CharacterDisconnected, bag, false);
            }

            if (ServerUser.TransferTarget == null || ServerUser.TransferTarget.Length == 0) // if we're not transferring
            {
                if (ServerUser.Profile != null && !ServerUser.IsAuthorizedClusterServer)
                {
                    ServerUser.Profile.SetLoggedOut();
                    if (ServerBase.UseDatabaseConnectivity && ServerUser.IsAuthenticated)
                    {
                        ServerUser.Profile.Save(MyServer.RequireAuthentication);
                    }
                }
                // kill auth ticket on this server.
                ConnectionManager.UnAuthorizeUser(ServerUser, false);
            }

            base.OnSocketKilled(msg);
        }

    }
}
