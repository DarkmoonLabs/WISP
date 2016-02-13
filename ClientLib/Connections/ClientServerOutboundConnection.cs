//#define UNITY
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace Shared
{
    public class ClientServerOutboundConnection
#if UNITY
        : UnityClientConnection
#else
        : ClientConnection
#endif
    {
        #region ServerReady Event
        private EventHandler ServerReadyInvoker;

        /// <summary>
        /// Fires when the server is ready for communication.
        /// </summary>
        public event EventHandler ServerReady
        {
            add
            {
                AddHandler_ServerReady(value);
            }
            remove
            {
                RemoveHandler_ServerReady(value);
            }
        }

        
        private void AddHandler_ServerReady(EventHandler value)
        {
            ServerReadyInvoker = (EventHandler)Delegate.Combine(ServerReadyInvoker, value);
        }

        
        private void RemoveHandler_ServerReady(EventHandler value)
        {
            ServerReadyInvoker = (EventHandler)Delegate.Remove(ServerReadyInvoker, value);
        }

        private void FireServerReady(object sender, EventArgs args)
        {
            if (ServerReadyInvoker != null)
            {
                ServerReadyInvoker(sender, args);
            }
        }
        #endregion
        
        #region AuthTicketRejected Event
        public delegate void AuthTicketRejectedDelegate(string msg);
        private AuthTicketRejectedDelegate AuthTicketRejectedInvoker;

        /// <summary>
        /// Fires when we tried to communicate with the server but the server doesn't (or no longer) has a valid authentication ticket registered for us
        /// </summary>
        public event AuthTicketRejectedDelegate AuthTicketRejected
        {
            add
            {
                AddHandler_AuthTicketRejected(value);
            }
            remove
            {
                RemoveHandler_AuthTicketRejected(value);
            }
        }

        
        private void AddHandler_AuthTicketRejected(AuthTicketRejectedDelegate value)
        {
            AuthTicketRejectedInvoker = (AuthTicketRejectedDelegate)Delegate.Combine(AuthTicketRejectedInvoker, value);
        }

        
        private void RemoveHandler_AuthTicketRejected(AuthTicketRejectedDelegate value)
        {
            AuthTicketRejectedInvoker = (AuthTicketRejectedDelegate)Delegate.Remove(AuthTicketRejectedInvoker, value);
        }

        private void FireAuthTicketRejected(string msg)
        {
            if (AuthTicketRejectedInvoker != null)
            {
                AuthTicketRejectedInvoker(msg);
            }
        }
        #endregion

        public ClientServerOutboundConnection(bool isBlocking) 
#if UNITY
            :base()
#else
            : base(isBlocking)
#endif
        {

        }

        protected override void OnPacketAuthFailed(PacketReply replyPacket)
        {
            base.OnPacketAuthFailed(replyPacket);
            FireAuthTicketRejected(replyPacket.ReplyMessage);
        }

        protected override void OnServerLoginResponse(PacketLoginResult packetLoginResult)
        {
            base.OnServerLoginResponse(packetLoginResult);
            
            if (packetLoginResult.ReplyCode == ReplyType.OK)
            {
                FireServerReady(this, EventArgs.Empty);
            }
            else if (packetLoginResult.ReplyCode != ReplyType.OK)
            {
                FireAuthTicketRejected(packetLoginResult.ReplyMessage);
            }
        }

        protected override void OnSocketKilled(string msg)
        {
            base.OnSocketKilled(msg);
            Client.ConnectionPhase = ClientConnectionPhase.Unconnected;
        }

        /// <summary>
        /// The auth ticket we use to connect to the game server
        /// </summary>
        public Guid AuthTicket { get; set; }

        /// <summary>
        /// Any misc paramaters that we want to pass to the server once the connection is made.
        /// </summary>
        public PropertyBag ConnectionParms { get; set; }
    }
}
