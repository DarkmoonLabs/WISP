using System;
using System.Collections.Generic;
using System.Text;
using ServerLib;
using System.Net;

namespace Shared
{
    /// <summary>
    /// Represents one user, logged into the server. 
    /// This user may also represents another server in the cluster logging in.
    /// </summary>
    public class ServerUser : Shared.User
    {
        public AccountProfile Profile { get; set; }

        /// <summary>
        /// The account/login name for this user
        /// </summary>
        new public string AccountName
        {
            get 
            { 
                return base.AccountName; 
            }
            set 
            {
                base.AccountName = value;
                
                Profile = new AccountProfile(value);
            }
        }

        private bool m_IsAuthorizedClusterServer = false;
        
        /// <summary>
        /// Is this "user" another server in the cluster?
        /// </summary>
        public bool IsAuthorizedClusterServer
        {
            get
            {
                return m_IsAuthorizedClusterServer;
            }
            set
            {
                m_IsAuthorizedClusterServer = value;
            }
        }

        /// <summary>
        /// The network connection for this user
        /// </summary>
        public INetworkConnection MyConnection { get; set; }

        /// <summary>
        /// The time when the authorization ticket expires.
        /// </summary>
        public DateTime AuthorizationExpires { get; set; }

#if !SILVERLIGHT

        /// <summary>
        /// Call this to renew the timer on the authorization ticket. You can call this whenever the user sends us any sort of packet.
        /// </summary>
        public void RenewAuthorizationTicket()
        {
            RenewAuthorizationTicket(false);
        }

        /// <summary>
        /// Call this to renew the timer on the authorization ticket. You can call this whenever the user sends us any sort of packet.
        /// </summary>
        public void RenewAuthorizationTicket(bool persist)
        {
            RenewAuthorizationTicket(persist, OwningServer);
        }

        /// <summary>
        /// Call this to renew the timer on the authorization ticket. You can call this whenever the user sends us any sort of packet.
        /// </summary>
        public void RenewAuthorizationTicket(bool persist, string authForWhichServer)
        {
            if (IsAuthorizedClusterServer)
            {
                AuthorizationExpires = DateTime.MaxValue;
            }
            else
            {
                if (!m_HasBeenTransferred) // if we've been transferred, i.e. told to go to another server, we will let the local ticket expire
                {
                    AuthorizationExpires = DateTime.UtcNow + ConnectionManager.AuthTicketLifetime;
                }

                if (persist)
                {
                    DB.Instance.User_AuthorizeSession(AccountName, ID, AuthTicket, OwningServer, DateTime.UtcNow, CurrentCharacter !=null? CurrentCharacter.ID : -1, authForWhichServer);
                }
            }
        }
#endif

        /// <summary>
        /// If a transfer request to another server in the cluster has been initiated and the player has been notified, they are considered
        /// to be transferring.  When a disconnection takes place while this property is True, we assume they are disconnecting due to that
        /// transfer.
        /// </summary>
        public string TransferTarget { get; set; }

        /// <summary>
        /// Players can get transferred around servers to consume services there.  Usually, there is one server
        /// that "owns" that character and keeps track of where it is.  This is that server's ServerUserID
        /// </summary>
        public string OwningServer { get; set; }

        public ServerUser()
            : base()
        {
            TransferTarget = null;
            OwningServer = "";
        }

        /// <summary>
        /// Attempts to transfer the player to a different server
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        /// <param name="authTicket">auth ticket for assisted transfers only.  unassisted transfers don't need this (Guid.Empty)</param>
        /// <param name="contentID"></param>
        /// <param name="friendlyServerName"></param>
        /// <returns></returns>
        public bool TransferToServerAssisted(string address, int port, Guid authTicket, Guid contentID, string friendlyServerName, string targetServerID, string msg, ReplyType reply)
        {
            if (MyConnection != null && MyConnection.IsAlive)
            {
                PacketGameServerTransferResult p = (PacketGameServerTransferResult) MyConnection.CreatePacket((int)PacketType.PacketGameServerAccessGranted, 0, true, true);
                p.AuthTicket = authTicket;
                p.ServerIP = address;
                p.ServerPort = port;

                p.IsAssistedTransfer = true;
                p.TargetResource = contentID;
                p.ServerName = friendlyServerName;
                p.ReplyCode = reply;
                p.ReplyMessage = msg;

                if (reply == ReplyType.OK)
                {
                    TransferTarget = targetServerID;
                }

                MyConnection.Send(p);
                return true;
            }

            return false;
        }

        public bool TransferToServerUnassisted(string address, int port, Guid contentID, string targetServerID, string targetServerName)
        {
            Log1.Logger("Server").Debug("Unassisted server transfer for user[" + AccountName + "] to [" + address + ":" + port.ToString() + ", " + targetServerID + "].");
            if (MyConnection != null && MyConnection.IsAlive)
            {
                RenewAuthorizationTicket(true, targetServerID);
                PacketGameServerTransferResult p = (PacketGameServerTransferResult)MyConnection.CreatePacket((int)PacketType.PacketGameServerAccessGranted, 0, true, true);
                p.AuthTicket = AuthTicket;
                p.ServerIP = address;
                p.ServerPort = port;

                p.IsAssistedTransfer = false;
                p.TargetResource = contentID;
                p.ServerName = targetServerName;
                p.ReplyCode = ReplyType.OK;
                p.ReplyMessage = "";

                //this.TransferTarget = "";
                this.TransferTarget = targetServerID;

                MyConnection.Send(p);

                // set expiration
                AuthorizationExpires = DateTime.UtcNow + TimeSpan.FromSeconds(15);
                m_HasBeenTransferred = true;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Has the player been transferred?
        /// </summary>
        private bool m_HasBeenTransferred = false;

        private ServerCharacterInfo m_CurrentCharacter;
        /// <summary>
        /// The character that this user is currently using.  A character is an abstracted version of a user.  All game related data information
        /// is tied to a character, not a user.  A user may, depending on implementation specifics, have multiple characters.  A character is essentially
        /// an abstracted user identity.
        /// </summary>
        public ServerCharacterInfo CurrentCharacter
        {
            get { return m_CurrentCharacter; }
            set 
            {
                if (m_CurrentCharacter != null)
                {
                    CharacterCache.UncacheCharacter(m_CurrentCharacter.CharacterInfo.ID);
                }
                m_CurrentCharacter = value;                
            }
        }
        

    }
}
