using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Runtime.CompilerServices;

namespace Shared
{
    public delegate void ServerLoginResultArrivedDelegate(IClientConnection sender, PacketLoginResult result);
    public delegate void ServerTransferDirectiveDelegate(INetworkConnection con, PacketGameServerTransferResult transfer);
    
    /// <summary>
    /// Base clase for all client connection.  A client connection is any connection that is OUTGOING.
    /// This class encapsulates one outgoing connection and handles remote host IP resolution as well as
    /// the ecnryption / decryption and key exchange with the target connection.
    /// </summary>
    public class ClientConnection : NetworkConnection, IClientConnection
    {
        #region Data

        /// <summary>
        ///Used to synchronize network time between this connection and another.
        /// </summary>
        public NetworkClock Clock { get; private set; }

#if !SILVERLIGHT
        /// <summary>
        /// Listens to all UDP traffic for our listen-on port.
        /// </summary>
        protected IUDPListener m_UDPListener;

        /// <summary>
        /// If set to true, will kick off the UDP keep alive packets which will ensure that the client side NAT keeps a UDP port open for us.
        /// Enabled by default.  Change setting with App.Config "EnableUDPKeepAlive = FALSE"
        /// </summary>
        private bool m_EnableUDPKeepAlive;
        /// <summary>
        /// If set to true, will kick off the UDP keep alive packets which will ensure that the client side NAT keeps a UDP port open for us.
        /// Enabled by default.  Change setting with App.Config "EnableUDPKeepAlive = FALSE"
        /// </summary>
        public bool EnableUDPKeepAlive
        {
            get { return m_EnableUDPKeepAlive; }
            set
            {
                m_EnableUDPKeepAlive = value;
                if (value)
                {
                    double natPokeInterval = ConfigHelper.GetIntConfig("NatPokeInterval", 20) * 1000; // miliseconds
                    if (natPokeInterval < 10000) // no faster than every 10 seconds
                    {
                        natPokeInterval = 10000;
                    }
                    m_UDPKeepAlive.Interval = natPokeInterval; // don't start it yet.  not until the connection is established.
                    m_UDPKeepAlive.Elapsed += new System.Timers.ElapsedEventHandler(OnUDPKeepAlive);
                }
                else
                {
                    m_UDPKeepAlive.Stop();
                    m_UDPKeepAlive.Elapsed -= new System.Timers.ElapsedEventHandler(OnUDPKeepAlive);
                }
            }
        }

        /// <summary>
        /// Send pings over UDP to keep the NAT open
        /// </summary>
        private System.Timers.Timer m_UDPKeepAlive = new System.Timers.Timer();

#endif

        private bool m_IsNewAccount = false;
        /// <summary>
        /// Should we try tro request this connection as a new account when logging in?
        /// </summary>
        public bool IsNewAccount
        {
            get { return m_IsNewAccount; }
            set { m_IsNewAccount = value; }
        }

        
        #region SocketSecured Event
        private EventHandler SocketSecuredInvoker;

        /// <summary>
        /// Signal that the connection handshake is complete and a Rijndael key exchange occurred successfully.
        /// We may now mark packets as encrypted.
        /// </summary>
        public event EventHandler SocketSecured
        {
            add
            {
                AddHandler_SocketSecured(value);
            }
            remove
            {
                RemoveHandler_SocketSecured(value);
            }
        }

        
        private void AddHandler_SocketSecured(EventHandler value)
        {
            SocketSecuredInvoker = (EventHandler)Delegate.Combine(SocketSecuredInvoker, value);
        }

        
        private void RemoveHandler_SocketSecured(EventHandler value)
        {
            SocketSecuredInvoker = (EventHandler)Delegate.Remove(SocketSecuredInvoker, value);
        }

        private void FireSocketSecured(object sender, EventArgs args)
        {
            if (SocketSecuredInvoker != null)
            {
                SocketSecuredInvoker(sender, args);
            }
        }
        #endregion

        #region SocketConnectionConcluded Event
        private Action<IClientConnection, bool, string> SocketConnectionConcludedInvoker;

        /// <summary>
        /// Fires when a connection attempt as been concluded and passes the result (true or false) and a
        /// message, if needed
        /// </summary>
        public event Action<IClientConnection, bool, string> SocketConnectionConcluded
        {
            add
            {
                AddHandler_SocketConnectionConcluded(value);
            }
            remove
            {
                RemoveHandler_SocketConnectionConcluded(value);
            }
        }

        
        private void AddHandler_SocketConnectionConcluded(Action<IClientConnection, bool, string> value)
        {
            SocketConnectionConcludedInvoker = (Action<IClientConnection, bool, string>)Delegate.Combine(SocketConnectionConcludedInvoker, value);
        }

        
        private void RemoveHandler_SocketConnectionConcluded(Action<IClientConnection, bool, string> value)
        {
            SocketConnectionConcludedInvoker = (Action<IClientConnection, bool, string>)Delegate.Remove(SocketConnectionConcludedInvoker, value);
        }

        private void FireSocketConnectionConcluded(IClientConnection con, bool result, string msg)
        {
            if (SocketConnectionConcludedInvoker != null)
            {
                SocketConnectionConcludedInvoker(con, result, msg);
            }
        }
        #endregion

        private bool m_ConnectionInProgress = false;

        /// <summary>
        /// Are we successfully logged in (and authenticated) to the target game server?
        /// </summary>
        public bool LoggedIn { get; set; }


        /// <summary>
        /// Connection attempts, when allowed by the platform, are asynchronous operations.  This property is true if
        /// a connection attempt is underway.  If a connection has been established, not attempted yet or disconnected, this property will be false.
        /// </summary>
        public bool ConnectionInProgress
        {
            get
            {
                return m_ConnectionInProgress;
            }
        }

        private string m_AccountName = "";
        /// <summary>
        /// The account name under which we will attempt to connect
        /// </summary>
        public string AccountName
        {
            get { return m_AccountName; }
            set { m_AccountName = value; }
        }

        private string m_Password = "";
        /// <summary>
        /// The password with which we will attempt to log in
        /// </summary>
        public string Password
        {
            get { return m_Password; }
            set { m_Password = value; }
        }

        private PacketLoginRequest.ConnectionType m_ConnectionType;
        /// <summary>
        /// How are we connecting?  Directly on our own accord, due to an assisted server transfer, unassisted server transfer, etc.
        /// This setting lets the target server know where it should look for our authentication ticket.
        /// </summary>
        public PacketLoginRequest.ConnectionType ConnectionType
        {
            get { return m_ConnectionType; }
        }
        

        /// <summary>
        /// The roles that the server says we have via this connection.
        /// </summary>
        public string[] Roles { get; set; }

        /// <summary>
        /// Tests to see if we are in a certain role, using this connection
        /// </summary>
        /// <param name="role"></param>
        /// <returns></returns>
        public bool IsInRole(string role)
        {
            return Array.IndexOf(Roles, role) > -1;
        }

        private string m_ServerAddress = "";
        private int m_Port = 0;

        private bool m_ConnectionWasInitiated = false;
        /// <summary>
        /// Will be true, if we ever tried to make a connection, regardless of outcome.
        /// </summary>
        public bool ConnectionWasInitiated
        {
            get
            {
                return m_ConnectionWasInitiated;
            }
        }

        private bool m_ConnectionWasKilled = false;
        /// <summary>
        /// Has this connection been destroyed?  If so, this client object should be discarded.  Attempting to reconnect
        /// a client connection that has been previously killed may lead to unpredictable behavior.
        /// </summary>
        public bool ConnectionWasKilled
        {
            get
            {
                return m_ConnectionWasKilled;
            }
        }

        #endregion

        #region ServerTransferDirective Event
        private ServerTransferDirectiveDelegate ServerTransferDirectiveInvoker;

        /// <summary>
        /// Fires when we're handed off to another server in the cluster.
        /// </summary>
        public event ServerTransferDirectiveDelegate ServerTransferDirective
        {
            add
            {
                AddHandler_ServerTransferDirective(value);
            }
            remove
            {
                RemoveHandler_ServerTransferDirective(value);
            }
        }

        
        private void AddHandler_ServerTransferDirective(ServerTransferDirectiveDelegate value)
        {
            ServerTransferDirectiveInvoker = (ServerTransferDirectiveDelegate)Delegate.Combine(ServerTransferDirectiveInvoker, value);
        }

        
        private void RemoveHandler_ServerTransferDirective(ServerTransferDirectiveDelegate value)
        {
            ServerTransferDirectiveInvoker = (ServerTransferDirectiveDelegate)Delegate.Remove(ServerTransferDirectiveInvoker, value);
        }

        private void FireServerTransferDirective(INetworkConnection con, PacketGameServerTransferResult result)
        {
            if (ServerTransferDirectiveInvoker != null)
            {
                ServerTransferDirectiveInvoker(con, result);
            }
        }
        #endregion

        #region ServerLoginResultArrived Event
        private ServerLoginResultArrivedDelegate ServerLoginResultArrivedInvoker;

        /// <summary>
        /// Fires when the login server responds to our login request.
        /// </summary>
        public event ServerLoginResultArrivedDelegate ServerLoginResultArrived
        {
            add
            {
                AddHandler_ServerLoginResultArrived(value);
            }
            remove
            {
                RemoveHandler_ServerLoginResultArrived(value);
            }
        }

        
        private void AddHandler_ServerLoginResultArrived(ServerLoginResultArrivedDelegate value)
        {
            ServerLoginResultArrivedInvoker = (ServerLoginResultArrivedDelegate)Delegate.Combine(ServerLoginResultArrivedInvoker, value);
        }

        
        private void RemoveHandler_ServerLoginResultArrived(ServerLoginResultArrivedDelegate value)
        {
            ServerLoginResultArrivedInvoker = (ServerLoginResultArrivedDelegate)Delegate.Remove(ServerLoginResultArrivedInvoker, value);
        }

        private void FireServerLoginResultArrived(IClientConnection con, PacketLoginResult result)
        {
            if (ServerLoginResultArrivedInvoker != null)
            {
                ServerLoginResultArrivedInvoker(con, result);
            }
        }
        #endregion

        public ClientConnection(bool isBlocking)
            : base(isBlocking)
        {
            Roles = new string[0];
            RegisterPacketHandler((int)PacketType.PacketRijndaelExchangeRequest, OnRijndaelExchangeRequest);
            RegisterPacketHandler((int)PacketType.LineSecured, OnLineSecured);
            RegisterPacketHandler((int)PacketType.LoginResult, OnLoginResult);
            RegisterPacketHandler((int)PacketType.PacketGameServerAccessGranted, OnGameServerAccessInfoArrived);

#if !SILVERLIGHT
            EnableUDPKeepAlive = ConfigHelper.GetStringConfig("EnableUDPKeepAlive", "TRUE").ToLower() == "true";
#endif
        }

        protected virtual void OnServerTransferDirective(PacketGameServerTransferResult msg)
        {
        }
        
        public event BeforeLoginRequestDelegate BeforeLoginRequest;
        public delegate void BeforeLoginRequestDelegate(PacketLoginRequest req);

        private void OnGameServerAccessInfoArrived(INetworkConnection con, Packet p)
        {            
            PacketGameServerTransferResult packetGameServerAccessGranted = p as PacketGameServerTransferResult;

            Log.LogMsg("Server Transfer to [" + packetGameServerAccessGranted.ReplyCode.ToString() + " / " + packetGameServerAccessGranted.ReplyMessage + " / " + packetGameServerAccessGranted.ServerName + " / " + packetGameServerAccessGranted.ServerIP + "] for resource [" + packetGameServerAccessGranted.TargetResource.ToString() + "].");            
            
            FireServerTransferDirective(this, packetGameServerAccessGranted);

            KillConnection(packetGameServerAccessGranted.ReplyCode == ReplyType.OK? "Transferred." : "Not Transferred.");
            OnServerTransferDirective(packetGameServerAccessGranted);            
        }
        

#if !SILVERLIGHT
        void OnUDPKeepAlive(object sender, System.Timers.ElapsedEventArgs e)
        {
            m_UDPKeepAlive.Stop();

            if (m_UDPListener != null && m_UDPListener.Port > 0)
            {
                SendUDPPoke(m_UDPListener.Port);
            }
            if (EnableUDPKeepAlive)
            {
                m_UDPKeepAlive.Start();
            }
        }

        /// <summary>
        /// In order to keep a UDP port open, every once in a while a packet has to be sent through the hole that was punched through the firewall.
        /// This is the time that the last UDP poke occurred.
        /// </summary>
        public DateTime LastUDPPokeSent = DateTime.MinValue;

        /// <summary>
        /// Sends a UDP null packet, to which the recipient will, at the least, reply to with an ACK
        /// which will cause a hole to be poked into the local NAT
        /// </summary>
        public bool SendUDPPoke(int thisListenOn)
        {
            PacketNATInfo keepAlive = (PacketNATInfo)CreatePacket((int)PacketType.NATInfo, 0, false, false);
            keepAlive.NeedsDeliveryAck = true;
            keepAlive.ListenOnPort = thisListenOn;
            keepAlive.IsUDP = true;
            bool rslt = Send(keepAlive) >= 0;
            SetLastUDPACKReceived(Clock.UTCTime);
            //Log.LogMsg("UDP poke to " + m_ServerAddress);
            return rslt;
        }
  
#endif

        private SocketAsyncEventArgs CreateNewSaeaForConnect()
        {
            //Allocate the SocketAsyncEventArgs object. 
            SocketAsyncEventArgs connectEventArg = new SocketAsyncEventArgs();
            connectEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(OnConnectEvent_Completed);
            return connectEventArg;
        }

#if !SILVERLIGHT

                /// <summary>
        /// Begin an asynchronous connection attempt
        /// </summary>
        /// <param name="serverAddress">the address of the target server.</param>
        /// <param name="port">the port of the target server</param>
        /// <param name="username">the username with which to authenticate, if any</param>
        /// <param name="password">the password with which to authenticate, if any</param>        
        public void BeginConnect(string serverAddress, int port, string username, string password)
        {
            BeginConnect(serverAddress, port, username, password, PacketLoginRequest.ConnectionType.DirectConnect);
        }

        /// <summary>
        /// Begin an asynchronous connection attempt
        /// </summary>
        /// <param name="serverAddress">the address of the target server.</param>
        /// <param name="port">the port of the target server</param>
        /// <param name="username">the username with which to authenticate, if any</param>
        /// <param name="password">the password with which to authenticate, if any</param>        
        public void BeginConnect(string serverAddress, int port, string username, string password, PacketLoginRequest.ConnectionType connectionType)
        {
            if (IsAlive || ConnectionInProgress || ConnectionWasInitiated)
            {
                return;
            }

            if (username == null)
            {
                username = "";
            }
            
            if (password == null)
            {
                password = "";
            }

            m_ConnectionType = connectionType;

            Log.LogMsg("Connecting to " + serverAddress + ":" + port.ToString() + " as " + username);
            m_ConnectionWasInitiated = true;
            m_ConnectionInProgress = true;
            AccountName = username;
            Password = password;
            m_ServerAddress = serverAddress;
            m_Port = port;            

            Log.LogMsg("Resolving host [" + serverAddress + "]");
            Dns.BeginGetHostAddresses(serverAddress, new AsyncCallback(OnHostResolve), null);
        }

        /// <summary>
        /// If ProcessPacketsImmediately == false, you must call this method to process all queued networking packets.
        /// No packets are ACKnowledged or replied to until this method is called. Calling this method clears out the inbox.
        /// </summary>
        /// <returns>the number of packets processed</returns>
        public override int ProcessNetworking()
        {
            if(!IsAlive)
            {
                Log.LogMsg("Connection is not alive.  Can't Process Networking.");
                return 0;
            }

            //Log.LogMsg("Before UDP Receive.  OwningConnection.IsAlive=" + IsAlive);
            if (m_UDPListener != null && m_UDPListener is UDPListenerDuplexBlocking)
            {
                ((UDPListenerDuplexBlocking)m_UDPListener).ProcessReceive();
            }

            int numProcessed = base.ProcessNetworking();
            return numProcessed;
        }

        private void OnHostResolve(IAsyncResult iar)
        {
            IPAddress[] host = null;
            try
            {
                host = Dns.EndGetHostAddresses(iar);

                // Check to make sure we got at least one IP address, otherwise we can stop right now.
                bool gotOne = false;
                IPAddress ip = null;
                bool useIP6 = ConfigHelper.GetStringConfig("UseIPv6").ToLower() == "true";
                foreach (IPAddress addy in host)
                {
                    if (useIP6 ? addy.AddressFamily == AddressFamily.InterNetworkV6 : addy.AddressFamily == AddressFamily.InterNetwork)
                    {
                        gotOne = true;
                        ip = addy;
                        break;
                    }
                }

                if (!gotOne)
                {
                    FireConnectedEvent(false, "Unable to resolve server IP Address.");
                }

                OnHostnameResolved(gotOne, ip);
                Log.LogMsg("Resolved host IP address to [" + ip + "]");

                IPEndPoint ipep = new IPEndPoint(ip, m_Port);
                SocketAsyncEventArgs connectEventArgs = CreateNewSaeaForConnect();
                MyTCPSocket = new Socket(useIP6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                connectEventArgs.AcceptSocket = MyTCPSocket;

                MyTCPSocket.Bind(new IPEndPoint(useIP6 ? IPAddress.IPv6Any : IPAddress.Any, 0));
                
                //MyUDPSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                //MyUDPSocket.ExclusiveAddressUse = false;

                connectEventArgs.RemoteEndPoint = ipep;
                MyTCPSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                MyTCPSocket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, DisableTCPDelay);
                //MyTCPSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.OutOfBandInline, true);

                Log.LogMsg("Connecting to host [" + ip + "] ...");
                bool willRaiseEvent = connectEventArgs.AcceptSocket.ConnectAsync(connectEventArgs);
                if (!willRaiseEvent)
                {
                    OnConnectionAttemptConcluded(connectEventArgs);
                }
            }
            catch (Exception e)
            {
                FireConnectedEvent(false, "Unable to resolve server IP Address. " + e.Message);
                return;
            }
        }
#else
                /// <summary>
        /// Begin an asynchronous connection attempt
        /// </summary>
        /// <param name="serverAddress">the address of the target server.</param>
        /// <param name="port">the port of the target server</param>
        /// <param name="username">the username with which to authenticate, if any</param>
        /// <param name="password">the password with which to authenticate, if any</param>
        /// <param name="listenForUDP">If this connection object should listen for UDP traffic (all connections can already send UDP data). On a server, this property should be false as servers make use of a global UDP listener.  On a client, this property should be true. This parameter is ignored on Silverlight.</param>
        public void BeginConnect(string serverAddress, int port, string username, string password)
        {
            BeginConnect(serverAddress, port, username, password, PacketLoginRequest.ConnectionType.DirectConnect);
        }

        /// <summary>
        /// Begin an asynchronous connection attempt
        /// </summary>
        /// <param name="serverAddress">the address of the target server.</param>
        /// <param name="port">the port of the target server</param>
        /// <param name="username">the username with which to authenticate, if any</param>
        /// <param name="password">the password with which to authenticate, if any</param>
        /// <param name="listenForUDP">If this connection object should listen for UDP traffic (all connections can already send UDP data). On a server, this property should be false as servers make use of a global UDP listener.  On a client, this property should be true. This parameter is ignored on Silverlight.</param>
        public void BeginConnect(string serverAddress, int port, string username, string password, PacketLoginRequest.ConnectionType connectionType)
        {
            if (IsAlive || ConnectionInProgress || ConnectionWasInitiated)
            {
                return;
            }

            if (username == null)
            {
                username = "";
            }
            if (password == null)
            {
                password = "";
            }

            m_ConnectionType = connectionType;

            Log.LogMsg("Connecting to " + serverAddress + ":" + port.ToString() + " as " + username);
            m_ConnectionWasInitiated = true;
            m_ConnectionInProgress = true;
            AccountName = username;
            Password = password;
            m_ServerAddress = serverAddress;
            m_Port = port;

            try
            {
                MyTCPSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                SocketAsyncEventArgs arg = CreateNewSaeaForConnect();
                DnsEndPoint dne = new DnsEndPoint(serverAddress, m_Port);
                arg.RemoteEndPoint = dne;

                arg.UserToken = MyTCPSocket;
                bool willRaiseEvent = MyTCPSocket.ConnectAsync(arg);
                if (!willRaiseEvent)
                {
                    OnConnectEvent_Completed(this, arg);
                }
            }
            catch (Exception e)
            {
                FireConnectedEvent(false, "Unable to resolve server IPAddress. " + e.Message);
                return;
            }
        }
#endif


        private void OnConnectEvent_Completed(object sender, SocketAsyncEventArgs e)
        {
            // determine which type of operation just completed and call the associated handler
            if (e.LastOperation == SocketAsyncOperation.Connect)
            {
                OnConnectionAttemptConcluded(e);
            }
            else
            {
                throw new ArgumentException("\r\nError in I/O Completed");
            }
        }

        private void OnConnectionAttemptConcluded(SocketAsyncEventArgs args)
        {
            args.Completed -= new EventHandler<SocketAsyncEventArgs>(OnConnectEvent_Completed);
            m_ConnectionInProgress = false;

            try
            {
                if (args.SocketError != SocketError.Success)
                {
                    FireConnectedEvent(false, args.SocketError.ToString());
                    return;
                }
            }
            catch (Exception conExc)
            {
                FireConnectedEvent(false, "Unable to connect to server: " + conExc.Message);
                return;
            }

            FireConnectedEvent(true, "Endpoint " + RemoteEndPoint.ToString());

            try
            {
                // send serviceId request 
                Log.LogMsg("Sending service ID " + ServiceID.ToString());
                Send(BitConverter.GetBytes(ServiceID), PacketFlags.IsCritical);

                if (!Transit.ListenForDataOnSocket())
                {
                    // ListenForDataOnSocket calls all appropriate kill events
                    KillConnection("Failed to listen on socket.");
                    return;
                }
            }
            catch (Exception con2Exc)
            {
                KillConnection(con2Exc.Message);
            }
        }

        private void InitiateUDP(IPEndPoint sendTarget)
        {
#if !SILVERLIGHT
            MyUDPSocket = new Socket(MyTCPSocket.AddressFamily == AddressFamily.InterNetworkV6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            // Must bind the UDP socket, even though we are not receiving with it (UDPListener receives)
            // Binding sets the appropriate local endpoint by which the remote
            // connection identifies all packets we send               
            UDPSendTarget = sendTarget;

            // Bind to new local address - do not share with TCP socket binding... does not work in Mono/Linux
            MyUDPSocket.Bind(new IPEndPoint(MyTCPSocket.AddressFamily == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any, 0));
            //MyUDPSocket.Bind(MyTCPSocket.LocalEndPoint);

            if (BlockingMode)
            {
                m_UDPListener = new UDPListenerDuplexBlocking();
                //m_UDPListener = new UDPListenerSimplex();
            }
            else
            {
                m_UDPListener = new UDPListener();
            }

            IPEndPoint local = MyUDPSocket.LocalEndPoint as IPEndPoint;
            m_UDPListener.Socket = MyUDPSocket;
            m_UDPListener.StartListening(MyUDPSocket.AddressFamily, local.Port, 2, this);

            if (LoggedIn && EnableUDPKeepAlive)
            {
                // poke TWICE! The first poke doesn't get responded to, because ACKs get sent
                // BEFORE a packet is processed... and the proper port wont be known until
                // AFTER the packet is processed.  So the first poke will be responded to on the
                // wrong port.
                OnUDPKeepAlive(null, null);
                OnUDPKeepAlive(null, null);
            }
#endif
        }

        private void FireConnectedEvent(bool success, string p_2)
        {
            m_ConnectionInProgress = false;
            if (!success)
            {
                KillConnection(p_2);
            }
            OnConnected(success, p_2);
        }

        #region Overrides & Virtuals

        /// <summary>
        /// Fires when the target address has been resolved.
        /// </summary>
        /// <param name="success">true, if the address could be resolved</param>
        /// <param name="resolvedAddress">the IP address, or null if not resolved</param>
        protected virtual void OnHostnameResolved(bool success, IPAddress resolvedAddress)
        {
        }

        /// <summary>
        /// Gets called when the socket for this connection makes the initial connection, i.e. the remote
        /// endpoint was successfully reached after having resolved it's IP address.
        /// </summary>
        /// <param name="success">true, if the connection was made, false if it failed</param>
        /// <param name="msg">a message explaining the result, if applicable</param>
        protected virtual void OnConnected(bool success, string msg)
        {
            FireSocketConnectionConcluded(this, success, msg);
        }

        /// <summary>
        /// Gets called when the socket for this connection was closed, for any reason
        /// </summary>
        /// <param name="msg"> a message explaining the socket closure, if any</param>
        protected override void OnSocketKilled(string msg)
        {
#if !SILVERLIGHT
            m_UDPKeepAlive.Elapsed -= new System.Timers.ElapsedEventHandler(OnUDPKeepAlive);
            m_UDPKeepAlive.Stop();
            m_UDPKeepAlive.Dispose();

            try
            {
                if (m_UDPListener != null)
                {
                    m_UDPListener.StopListening();
                    if (m_UDPListener is UDPListenerSimplex)
                    {
                        ((UDPListenerSimplex)m_UDPListener).StopNetworkingPump();
                    }
                    m_UDPListener = null;
                }
            }
            catch { }
#endif

            base.OnSocketKilled(msg);
            m_ConnectionInProgress = false;
            m_ConnectionWasKilled = true;
        }

        protected override byte[] OnBeforeSendPacket(byte[] body, bool encrypt, bool compress, Pointer bodyPointer)
        {
            if (encrypt && m_ConnectionKey.Length > 0)
            {
                // Log.LogMsg("Pre Encryption packet size " + body.Length.ToString());
                body = CryptoManager.RijEncrypt(body, 0, bodyPointer.Position, m_ConnectionKey);
                // Log.LogMsg("Encrypted packet size " + body.Length.ToString());
                bodyPointer.Position = body.Length;
            }

            if (compress)
            {
                body = Compression.CompressData(body, bodyPointer);
            }

            return body;
        }

        protected override void OnClockSync(PacketClockSync msg)
        {
            Clock.AddSample(msg.StartTime, DateTime.UtcNow.Ticks, msg.SentOnUTC);
        }

        protected override void TimestampOutgoingPacket(Packet msg)
        {
            if (Clock != null)
            {
                msg.SentOnUTC = Clock.UTCTimeTicks;
            }
            else
            {
                msg.SentOnUTC = DateTime.UtcNow.Ticks;
            }
        }

        #endregion

        #region Handlers


        private void OnRijndaelExchangeRequest(INetworkConnection con, Packet msg)
        {
            PacketRijndaelExchangeRequest p = msg as PacketRijndaelExchangeRequest;
            // Server said hello.  Generate, encrypt with public RSA key and finally send the key.  this will be our 
            // connection key for as long as this connection is valid

            // Generate & Store new key
            m_ConnectionKey = CryptoManager.GetRandomRijndaelKey();
            RemoteRsaKey = p.PublicRSAKey;
            // Encrypt it with the public RSA key from the server
            byte[] encryptedKey = CryptoManager.EncryptRijndaelKey(p.PublicRSAKey, m_ConnectionKey);

            // Send it
            PacketRijndaelExchange re = (PacketRijndaelExchange)CreatePacket((int)PacketType.RijndaelExchange, 0, false, false);
            re.RijndaelExchangeData = encryptedKey;
            re.PublicRSAKey = CryptoManager.PublicRSAKey;
            re.ReplyCode = m_ConnectionKey != null && m_ConnectionKey.Length > 0 ? ReplyType.OK : ReplyType.Failure;
            re.ReplyPacketType = msg.PacketTypeID;
            re.ReplyPacketID = msg.PacketID;

            msg.ReplyPacket = re;
        }

        private void OnLineSecured(INetworkConnection con, Packet p)
        {
            PacketLineSecured msg = p as PacketLineSecured;
            Log.LogMsg("Got Rijndael reply. Verifying key...");
            // Server got our encrypted Rijndael key.  Make sure it's all good still.
            if (m_ConnectionKey.Equals(CryptoManager.RijDecrypt(msg.Key, m_ConnectionKey)) && msg.ReplyCode == ReplyType.OK)
            {
                KillConnection("Failed to secure the connection.  Closing socket. " + msg.ReplyMessage);
                return;
            }
            Log.LogMsg("Line secured. Sending login request.");

            // FireLineSecuredEvent
            FireSocketSecured(this, EventArgs.Empty);

            // Send the login request, encrypting our user name and password
            PacketLoginRequest plr = (PacketLoginRequest)CreatePacket((int)PacketType.LoginRequest, 0, true, true);
            plr.AccountName = m_AccountName;
            plr.Password = m_Password;
            plr.IsNewAccount = IsNewAccount;
            plr.LoginConnectionType = m_ConnectionType;
            OnBeforeLoginRequest(plr);
            Send(plr);
        }

        /// <summary>
        /// Gets called before the login request packet is sent. Use this opportunity to modify the packet or add additional parameters to the packet.
        /// </summary>
        /// <param name="req"></param>
        protected virtual void OnBeforeLoginRequest(PacketLoginRequest req)
        {
            if (BeforeLoginRequest != null)
            {
                BeforeLoginRequest(req);
            }
        }

        /// <summary>
        /// The server we are connecting to has resolved our login request
        /// </summary>
        /// <param name="packetLoginResult"></param>
        private void OnLoginResult(INetworkConnection con, Packet packetLoginResult)
        {
            PacketLoginResult msg = packetLoginResult as PacketLoginResult;
            LoggedIn = msg.ReplyCode == ReplyType.OK;
            OnServerLoginResponse(msg);

            if (LoggedIn)
            {
                Clock.SyncEnabled = false;// ConfigHelper.GetStringConfig("SynchronizeClockWithServer", "TRUE").ToUpper() == "TRUE";
            }
        }

        /// <summary>
        /// Gets called when the server we are connecting to has resolved our login request
        /// </summary>
        protected virtual void OnServerLoginResponse(PacketLoginResult result)
        {
            try
            {
                if (result.ReplyCode == ReplyType.OK)
                {
                    Roles = result.Parms.GetStringArrayProperty(-1);

                    int numSamplesForClockSync = ConfigHelper.GetIntConfig("NumSamplesForClockSync", 10); // 10 samples
                    int syncTimeAllowed = ConfigHelper.GetIntConfig("ClockSyncTimeAllowed", 15000); // across 15 seconds
                    int timeBetweenSyncs = ConfigHelper.GetIntConfig("TimeBetweenClockSync", 0); // only sync once per session
                    Clock = new NetworkClock(this, numSamplesForClockSync, syncTimeAllowed, timeBetweenSyncs);
                }

                FireServerLoginResultArrived(this, result);
            }
            catch (Exception e)
            {
                Log.LogMsg("Login failure. " + e.Message);
            }
        }

        #endregion

        
    }
}
