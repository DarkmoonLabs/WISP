using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO.Compression;
using System.Timers;

namespace Shared
{
    /// <summary>
    /// Represents one user connecting to the server.
    /// </summary>
    public class InboundConnection : NetworkConnection
    {
        public delegate void DisconnectedDelegate(InboundConnection con, string msg);

        /// <summary>
        /// Fires when the connection is broken.
        /// </summary>
        public event DisconnectedDelegate Disconnected;
        
        /// <summary>
        /// Servers in the same cluster authenticate each other by a shared password.
        /// </summary>
        protected static Guid SharedSecretWithClusterServers { get; set; }
        
        /// <summary>
        /// The number of Inbound connection in memory.
        /// </summary>
        public static int NUM_CONNECTIONS_IN_MEMORY = 0;

        /// <summary>
        /// The server to which this connection is attached to
        /// </summary>
        public ServerBase MyServer { get; set; }

        /// <summary>
        /// Connections that don't get used, get shut down.  This timer handles that.  Timeout length specified in App.config
        /// </summary>
        private Timer m_TimeoutTimer = null;

        /// <summary>
        /// The user assoaciated with this connection
        /// </summary>
        protected ServerUser m_ServerUser;

        /// <summary>
        /// The ServerUser object associated with this connection
        /// </summary>
        public ServerUser ServerUser
        {
            get { return m_ServerUser; }
            set 
            { 
                m_ServerUser = value; 
            }
        }
        
        ~InboundConnection()
        {
            NUM_CONNECTIONS_IN_MEMORY--;
        }

        protected override void OnBytesSent(int amount)
        {
            PerfMon.IncrementCustomCounter("Bandwidth Out", amount);
        }

        protected override void OnBytesReceived(int amount)
        {
            PerfMon.IncrementCustomCounter("Bandwidth In", amount);
        }

        protected override void OnPacketSent()
        {
            PerfMon.IncrementCustomCounter("Packets Out", 1);
        }
         
        protected override void OnPacketReceived()
        {
            PerfMon.IncrementCustomCounter("Packets In", 1);
        }

        static InboundConnection()
        {
            string key = ConfigHelper.GetStringConfig("SharedKeyWithClusterServers");
            if (key.Length > 0)
            {
                SharedSecretWithClusterServers = new Guid(key);
            }
            else
            {
                SharedSecretWithClusterServers = Guid.Empty;
            }
        }

        private void InitiateUDP(IPEndPoint sendTarget)
        {
#if !SILVERLIGHT
            MyUDPSocket = new Socket(MyTCPSocket.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            MyUDPSocket.ExclusiveAddressUse = false;
            MyUDPSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            IPAddress addy = IPAddress.Any;
            if (MyTCPSocket.AddressFamily == AddressFamily.InterNetworkV6)
            {
                addy = IPAddress.IPv6Any;
            }
            MyUDPSocket.Bind(new IPEndPoint(addy, MyServer.ListenOnPort)); // must bind so our local port is known to those who receive our UDP packets.
#endif
        }

        public InboundConnection(Socket s, ServerBase server, bool isBlocking) : base(isBlocking)
        {
            MyServer = server;
            NUM_CONNECTIONS_IN_MEMORY++;
            try
            {
                MyTCPSocket = s;
                MyTCPSocket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, DisableTCPDelay);
   
                m_TimeoutTimer = new Timer();

                int timeout = ConfigHelper.GetIntConfig("PlayerConnectionTimeout");
                if (timeout < 1)
                {
                    timeout = 10;
                }

                m_TimeoutTimer.Interval = timeout * 1000;
                m_TimeoutTimer.Elapsed += new ElapsedEventHandler(TimeoutTimer_Elapsed);
                m_TimeoutTimer.Start();

                ServerUser = new Shared.ServerUser();
                ServerUser.MyConnection = this;

                // Track the network socket associated with this connection object.
                string msg = "";
                if (!ConnectionManager.TrackUserSocket(this, ref msg))
                {
                    KillConnection(msg);
                    return;
                }

                if (!Transit.ListenForDataOnSocket())
                {
                    KillConnection("Remote end closed socket.");
                    return;
                }

                Log1.Logger("Server").Info("Now have [" + ConnectionManager.ConnectionCount.ToString() + " connections] attached.");
            }
            catch (Exception e)
            {
                KillConnection("Error instantiating Inbound connection object " + GetType().ToString() + " : " + e.Message);
                Log1.Logger("Server.Network").Error("Error instantiating Inbound connection object " + GetType().ToString() + " : " + e.Message, e);
                return;
            }
            SendRijndaelExchangeRequest();

        }

        /// <summary>
        /// If it's a UDP null packet, assume it's a keep-alive 
        /// </summary>
        private void OnNATInfo(INetworkConnection con, Packet msg)
        {
            //Log1.Logger("Server.Network").Debug("Got NAT poke from " + ((InboundConnection)con).ServerUser.AccountName);
            // update nat info
            IPEndPoint tcpep = null;
            try
            {
                tcpep = MyTCPSocket.RemoteEndPoint as IPEndPoint;
                if (tcpep == null)
                {
                    return;
                }
            }
            catch 
            {
                return;
            }

            UDPSendTarget = new IPEndPoint(tcpep.Address, ((PacketNATInfo)msg).ListenOnPort);                        
        }

        /// <summary>
        /// Fires when this connection hasn't had any activitity on it.  Timeout duration specified in app.config.
        /// </summary>
        void TimeoutTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (this.ServerUser.Profile != null && this.ServerUser.Profile.IsUserInRole("Administrator"))
            {
                return;
            }
            m_TimeoutTimer.Stop();
            KillConnection("User Connection timed out for " + this.ServerUser.AccountName + " : " + this.RemoteIP);
        }

        /// <summary>
        /// First thing that happens when a connection inbounds.  The server always starts the conversation.  
        /// </summary>
        private void SendRijndaelExchangeRequest()
        {
            PacketRijndaelExchangeRequest p = (PacketRijndaelExchangeRequest)CreatePacket((int)PacketType.PacketRijndaelExchangeRequest, 0, false, false);
            p.NeedsReply = true;
            p.PublicRSAKey = CryptoManager.PublicRSAKey;
            p.ConnectionKeySize = 128;
            Send(p);
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
                
                m_IsInitialized = true;
            }
            
            RegisterPacketHandler((int)PacketType.RijndaelExchange, OnRijndaelExchange);
            RegisterPacketHandler((int)PacketType.NATInfo, OnNATInfo);
        }               

        #region Networking
       
        public string RemoteIP
        {
            get
            {
                return ((IPEndPoint)RemoteEndPoint).Address.ToString();
            }
        }

        protected override byte[] OnBeforeSendPacket(byte[] body, bool encrypt, bool compress, Pointer bodyPointer)
        {            
            if (encrypt && m_ConnectionKey.Length > 0)
            {
                body = CryptoManager.RijEncrypt(body, 0, bodyPointer.Position, m_ConnectionKey);
                bodyPointer.Position = body.Length;
            }

            if (compress)
            {
                body = Compression.CompressData(body, bodyPointer);
            }

            return body;
        }        

        #endregion

        public int LatencyMs { get; set; }

        protected override void OnClockSync(PacketClockSync msg)
        {
            // reply with the server time, but use the same packet to reply
            // in order to preserve the original timestamps
            long now = DateTime.UtcNow.Ticks;

            try
            {
                LatencyMs = (int)Math.Ceiling(TimeSpan.FromTicks((now - msg.SentOnUTC) * 2).TotalMilliseconds);
                //Log1.Logger("Server.Network").Debug("Clock sync request. " + LatencyMs + "ms latency.");
            }
            catch{}

            msg.ReplyPacket = msg;
            msg.PacketID = Packet.NextPacketId; // give it a new packet id
            // packet will be time stamped on send
        }

        public override bool OnBeforePacketProcessed(Packet p)
        {            
            // if the base class doesn't think we should process the packet, then we don't think so either. 
            if (!base.OnBeforePacketProcessed(p)) // check for duplicate requests and send ACK if requested
            {
                return false;
            }
          
            // Check for authorization.  The only packet types that do not require authorization are ServerGreeting, RijndaelExchange, LineSecured, LoginRequest/Result, Null and Reply
            // these are defined as -1000 to -994.
            if (p.PacketTypeID < -1000 || p.PacketTypeID > -994 && !ServerUser.IsAuthorizedClusterServer)
            {
                if (ServerUser.AuthorizationExpires < DateTime.UtcNow)
                {
                    PacketReply rep = CreateStandardReply(p, ReplyType.AuthorizationTicketExpired, "Not authorized.");
                    p.NeedsReply = true;
                    p.ReplyPacket = rep;
                    Send(rep);
                    KillConnection("Authorization expired.");
                    return false; // prevent calling handler for this packet, since we're not authorized
                }
                else
                {
                    ServerUser.RenewAuthorizationTicket();
                }
            }

            if (p.PacketTypeID != (int)PacketType.ACK && p.PacketTypeID != (int)PacketType.NATInfo)
            {
                m_TimeoutTimer.Stop();
            }
            return true;
        }

        public override void OnAfterPacketProcessed(Packet p)
        {

            base.OnAfterPacketProcessed(p); // handles duplicate request safeguards and sending of the reply packet

            if (p.ReplyPacket != null && p.ReplyPacket.ReplyCode == ReplyType.AuthorizationTicketExpired)
            {
                KillConnection("Disconnecting client. " + p.ReplyPacket.ReplyMessage);
                return;
            }

            if (ServerUser != null && ServerUser.IsAuthorizedClusterServer)
            {
               return;
            }

            m_TimeoutTimer.Start();
        }

        /// <summary>
        /// Encryption helper method. 
        /// </summary>
        private void OnRijndaelExchange(INetworkConnection con, Packet pck)
        {
            PacketRijndaelExchange msg = pck as PacketRijndaelExchange;
            try
            {
                m_ConnectionKey = CryptoManager.DecryptRijndaelKey(msg.RijndaelExchangeData);
                PacketLineSecured p = (PacketLineSecured)CreatePacket((int)PacketType.LineSecured, 0, false, true);
                p.Key = CryptoManager.RijEncrypt(m_ConnectionKey, 0, m_ConnectionKey.Length, m_ConnectionKey);
                p.ReplyCode = ReplyType.OK;
                msg.ReplyPacket = p;
                RemoteRsaKey = msg.PublicRSAKey;
                
            }
            catch (Exception e)
            {
                KillConnection("Encryption key exchange error. Disconnecting client connection. " + e.Message);
            }
        }        

        protected virtual void OnParentConnectionSet()
        {
        }
        
        /// <summary>
        /// Connection broke for some reason.
        /// </summary>
        protected override void OnSocketKilled(string msg)
        {
            try
            {
                if (m_TimeoutTimer != null)
                {
                    m_TimeoutTimer.Stop();
                }

                base.OnSocketKilled(msg);
                if (Disconnected != null)
                {
                    Disconnected(this, msg);
                }

                // The user disconnected.  If they are transferring somewhere, then we're good to go.  If they are not transferring, then
                // we lost them and they are technically logged off.

                if (ServerUser.TransferTarget == null || ServerUser.TransferTarget.Length < 1)
                {                    
                    if (MyServer.UseCharacters && !MyServer.RequireAuthentication && ServerUser.CurrentCharacter != null)
                    {
                        // we use temp characters. go ahead and nuke it since they logged off and there is no way to recover the character.                        
                        string msg1 = "";
                        if (!CharacterUtil.Instance.DeleteCharacter(ServerUser.CurrentCharacter.ID, ServerUser, false, "Temp character and unauthenticated owner logged off.", ref msg1))
                        {
                            Log1.Logger(MyServer.ServerUserID).Error("User appears to have logged off. Failed to delete their temp character [" + ServerUser.CurrentCharacter.CharacterName + "], ID [" + ServerUser.CurrentCharacter.ID + "]. " + msg1);
                        }
                        else
                        {
                            Log1.Logger(MyServer.ServerUserID).Info("User appears to have logged off. Deleted temp character [" + ServerUser.CurrentCharacter.CharacterName + "], ID [" + ServerUser.CurrentCharacter.ID + "] since it can't be recovered (server does not use accounts).");
                        }
                    }
                }
            }
            catch
            { }
            finally
            {
                if (!ConnectionManager.RemoveConnection(this.UID))
                {
                    int x = 0;
                }
            }

            Log1.Logger("Server").Info("Now have [" + ConnectionManager.ConnectionCount.ToString() + " connections] attached.");
        }      

    }
}
