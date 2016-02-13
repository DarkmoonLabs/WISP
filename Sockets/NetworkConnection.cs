using System;
using System.Collections;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Shared
{
    public delegate void SocketKilledDelegate(object sender, string msg);

    /// <summary>
    /// Encapsulates one socket connection.  All lower level socket communication plumbing is handled by this class.
    /// This class is abstract and so can't be instantiated without inheriting.
    /// </summary>
    public abstract partial class NetworkConnection : IDisposable, INetworkConnection
    {
        #region Hash
        private static long m_Hash = 0;
        private int m_HashCode = -1;
        public override int GetHashCode()
        {
            if (m_HashCode == -1)
            {
                m_HashCode = Interlocked.Increment(ref m_HashCode);
            }

            return m_HashCode;
        }
        #endregion

        protected CryptoManager CryptoManager = new CryptoManager();

        private Guid m_UID = Guid.Empty;

        /// <summary>
        /// Unique ID for this network connection
        /// </summary>
        public Guid UID
        {
            get 
            { 
                return m_UID; 
            }
        }


        private bool m_CanSendUDP = false;
        public bool CanSendUDP
        {
            get { return m_CanSendUDP; }
            set { m_CanSendUDP = value; }
        }

        private static bool? m_DisableTCPDelay = null;
        /// <summary>
        /// Is App.Config option "DisableTCPDelay" set?
        /// </summary>
        public static bool DisableTCPDelay
        {
            get
            {
                if (m_DisableTCPDelay == null)
                {
                    m_DisableTCPDelay = ConfigHelper.GetStringConfig("DisableTCPDelay").ToLower() == "true";
                }

                return m_DisableTCPDelay.GetValueOrDefault(false);
            }
            set { m_DisableTCPDelay = value; }
        }

        private static bool? m_ProcessIncomingPacketsImmediately = null;
        /// <summary>
        /// If true, packets are processed as they are received, which is always an asyncronous action.  In some clients this can cause a problem,
        /// particularly in tightly looped 3d game clients.  For tightly looped clients, it is generally recommended that this setting be set to "False"
        /// and then to call the "ProcessNetworking" method when you are ready to process networking messages in your game loop. For all other types of clients, such 
        /// as Silverlight clients for instance, processing immediately should not present any problems.  This setting can also be controlled via the
        /// App.Config setting "ProcessPacketsImmediately=TRUE/FALSE".  The default setting is true.
        /// </summary>
        public bool ProcessIncomingPacketsImmediately
        {
            get
            {
                if (m_ProcessIncomingPacketsImmediately == null)
                {
                    m_ProcessIncomingPacketsImmediately = ConfigHelper.GetStringConfig("ProcessPacketsImmediately", "true").ToLower() == "true";
                }

                return m_ProcessIncomingPacketsImmediately.GetValueOrDefault(true);
            }
            set
            {
                if (m_ProcessIncomingPacketsImmediately == value)
                {
                    return;
                }

                m_ProcessIncomingPacketsImmediately = value;
                if (!value)
                {
                    Log.LogMsg("Now queueing incoming packets.");
                    m_Inbox = new Queue<Packet>(256);
                }
                else
                {
                    Log.LogMsg("Now processing incoming packets immediately.");
                    // process anything in the queue
                    lock (m_InboxLocker)
                    {
                        ProcessNetworking();
                        // release!
                        m_Inbox.Clear();
                        m_Inbox = null;
                    }
                }
            }
        }

        /// <summary>
        /// Used to store incoming packets in the event that ProcessPacketsImmediately == false
        /// </summary>
        private Queue<Packet> m_Inbox = new Queue<Packet>(1024);

        /// <summary>
        /// Synchronization object
        /// </summary>
        private object m_InboxLocker = new object();

        /// <summary>
        /// If ProcessPacketsImmediately == false, you must call this method to process all queued networking packets.
        /// No packets are ACKnowledged or replied to until this method is called. Calling this method clears out the inbox.
        /// </summary>
        /// <returns>the number of packets processed</returns>
        public virtual int ProcessNetworking()
        {
            try
            {
                if (Transit != null)
                {
                    Transit.ProcessSend();
                    Transit.ProcessReceive();
                }
                else
                {
                    Log.LogMsg("Transit is null. No networking to process.");
                }
            }
            catch(Exception e)
            {
                Log.LogMsg("Exception thrown while processing networking. " + e.Message + "\r\n" + e.StackTrace);
            }

            int processed = 0;
            lock (m_InboxLocker)
            {
                if (m_Inbox == null)
                {
                    return -1;
                }
             
                while (m_Inbox.Count > 0)
                {
                    Packet p = m_Inbox.Dequeue();
                    HandlePacket(p);
                    processed++;
                }
            }

            return processed;
        }

        

        private Socket m_MyTCPSocket;
        public Socket MyTCPSocket
        {
            set
            {
                m_MyTCPSocket = value;

                if (value != null)
                {
                    SetTransitStrategy();
                    Transit.InitTCP();
                }
            }
            get
            {
                return m_MyTCPSocket;
            }
        }

        private Socket m_MyUDPSocket;

        private IPEndPoint m_SendTarget = new IPEndPoint(IPAddress.None, 0);
        public IPEndPoint UDPSendTarget
        {
            get { return m_SendTarget; }
            set
            {
                m_SendTarget = value;
                m_CanSendUDP = true;
            }
        }

        protected void SetTransitStrategy()
        {
            if(Transit == null)
            {
#if !SILVERLIGHT
                if (m_BlockingMode)
                {
                    Log.LogMsg("Initializing Duplex Blocking Network Transit.");
                    Transit = new SimplexAsyncTransitStrategy(this);
                    //Transit = new DuplexBlockingTransitStrategy(this);
                }
                else
#endif
                {
                    Log.LogMsg("Initializing Duplex Async Network Transit.");
                    Transit = new DuplexAsynchTransitStrategy(this);
                }
            }
        }

        public Socket MyUDPSocket
        {
            set
            {
                m_MyUDPSocket = value;
                if (value != null)
                {
                    SetTransitStrategy();
                    Transit.InitUDP();                   
                }
            }
            get
            {
                return m_MyUDPSocket;
            }
        }

        private PacketHandlerMap m_PacketHandlerMap = new PacketHandlerMap();
        public static FactoryMap PacketCreationMap = new FactoryMap();

        public NetworkConnection(bool isBlocking)
        {
            m_UID = Guid.NewGuid();

            m_BlockingMode = isBlocking;
            StandardReplyHandlerMap = new PacketHandlerMap();

            OnInitialize();

            NetworkConnection.m_ConnectionsInExistence++;
            Log.LogMsg("Connection allocated. " + NetworkConnection.m_ConnectionsInExistence + " connection(s) in memory.");
        }

        ~NetworkConnection()
        {
            NetworkConnection.m_ConnectionsInExistence--;
#if !SILVERLIGHT
            if (Transit != null && Transit is SimplexAsyncTransitStrategy)
            {
                ((SimplexAsyncTransitStrategy)Transit).StopNetworkingPump();
            }
#endif
            Log.LogMsg("Connection resources free'd. " + NetworkConnection.m_ConnectionsInExistence + " connection(s) left in memory.");
        }

        static NetworkConnection()
        {
            PacketCreationMap = new FactoryMap();
        }

        // *********************************
        // *********** EVENTS **************
        // *********************************

        
        #region SocketKilled Event
        private SocketKilledDelegate SocketKilledInvoker;
        /// <summary>
        /// Signal that this socket has been shut down and killed. remove all references to this socket, if any.
        /// This socket will never ever send or receive data gain.
        /// </summary>
        public event SocketKilledDelegate SocketKilled
        {
            add
            {
                AddHandler_SocketKilled(value);
            }
            remove
            {
                RemoveHandler_SocketKilled(value);
            }
        }

        
        private void AddHandler_SocketKilled(SocketKilledDelegate value)
        {
            SocketKilledInvoker = (SocketKilledDelegate)Delegate.Combine(SocketKilledInvoker, value);
        }

        
        private void RemoveHandler_SocketKilled(SocketKilledDelegate value)
        {
            SocketKilledInvoker = (SocketKilledDelegate)Delegate.Remove(SocketKilledInvoker, value);
        }

        private void FireSocketKilled(object sender, string msg)
        {
            if (SocketKilledInvoker != null)
            {
                SocketKilledInvoker(sender, msg);
            }
        }
        #endregion


        // ********************************
        // ************* DATA *************
        // ********************************

        // For reading incoming packets
        #region Data

        private int m_LastTCPPacketIdProcessed = 0;

        /// <summary>
        /// A connection may send the same packet (same packet id) multiple times (some connection types will repeat a packet send
        /// if the packet hasn't been acknowledged in a timely manner or if the packet was previously rejected due to non-authorization).
        /// Sometimes the packet was processed, but the calling connection didn't receive the acknoledgement.  In such an event, the calling connection
        /// may erroneously re-issue the packet thinking it has not been processed yet, which could lead to an undersirable state where a command gets executed
        /// multiple times.  Set this property to true, to prevent a command from being re-processed multiple times.  Basically, setting this to true will create
        /// a log of the last 25 packet IDs that have been processed and will simply re-send ACK replies (instead of processing the command) if a packet ID in the 
        /// log is received again. 
        /// <para>
        /// As there are minor performance implication to turning this feature on (resource locking, list searching and a few boolean checks) the default setting for this
        /// property is FALSE.
        /// </para>
        /// </summary>
        public bool PreventRepeatedPacketProcessing = false;

        /// <summary>
        /// Used to lock access to the packet log where we store the last several packets that were processed
        /// </summary>
        private int m_UpdatingPacketLog = 0;

        /// <summary>
        /// History of the last few packets that were processed on this connection. Mostly this is used 
        /// to make sure that if the client transmits a request multiple times, that it doesnt get
        /// processed multiple times
        /// </summary>
        private List<int> m_LastFewPacketsAnswered = new List<int>();


        private bool m_BlockingMode = false;
        /// <summary>
        /// Sockets in blocking mode don't send packet asynchronously.  Packets are never dropped.
        /// Will try to send a packet until either it goes through or the connection dies.
        /// </summary>
        public bool BlockingMode
        {
            get { return m_BlockingMode; }
            set { m_BlockingMode = value; }
        }

        private PacketHandlerMap m_StandardReplyHandlerMap = new PacketHandlerMap();
        //private Dictionary<int, Action<NetworkConnection, PacketReply>> m_StandardReplyHandlerMap;
        /// <summary>
        /// Stores delegates to create packets, based on the packet type 
        /// </summary>
        private PacketHandlerMap StandardReplyHandlerMap
        {
            get { return m_StandardReplyHandlerMap; }
            set { m_StandardReplyHandlerMap = value; }
        }

        private static int m_ConnectionsInExistence = 0;

        /// <summary>
        /// The total number of ConnectionBase objects in existence (assuming inheritors called base() in their constructors).
        /// Note that this is the number of connections in memory, not actually being used.  When the garbage collector
        /// frees a ConnectionBase destructor is called (i.e. this object is garbage collected), this count will decrement.
        /// </summary>
        public static int ConnectionsInExistence
        {
            get
            {
                return m_ConnectionsInExistence;
            }

            set
            {
                m_ConnectionsInExistence = value;
            }

        }

        /// <summary>
        /// A connection may very briefly be connected (IsConnected = true) but also be 
        /// in the process of shutting down (ShuttingDown = true), making the connection
        /// unsuitable for communication. Use this IsAlive property
        /// to see if a connection is usable.  This property is a convenience accessor which
        /// checks (IsConnected == true && !ShuttingDown)
        /// </summary>
        public bool IsAlive
        {
            get
            {
                return IsConnected && !ShuttingDown;
            }
        }

        /// <summary>
        /// Indicates wether or not the socket is connected. Note, that a connection may very briefly be connected (IsConnected == true) but also be 
        /// in the process of shutting down (ShuttingDown = true). Use ConnectionBase.IsAlive to see if a connection is suitable for 
        /// communication.
        /// </summary>
        /// <returns>connected or not</returns>
        public bool IsConnected
        {
            get
            {
                if (this.m_ShuttingDown == 1)
                {
                    return false;
                }

                if (MyTCPSocket != null && MyTCPSocket.Connected)
                {
                    return true;
                }

                return false;
            }
        }

        private long m_ShuttingDown = 0;
        /// <summary>
        /// If the connection is in the process of shutting down.  This state generally only persists for a few miliseconds.
        /// </summary>
        public bool ShuttingDown 
        {
            get
            {
#if SILVERLIGHT
                long val = Interlocked.CompareExchange(ref m_ShuttingDown, 0, 0);
                return val == 1;
#else
                return Interlocked.Read(ref m_ShuttingDown) == 1;
#endif
            }
        }

        private int m_ServiceID = 0;
        /// <summary>
        /// The type of communication (service) that this connection represents.  This will determine what subclass of Inbound/Outbound Connection
        /// this object should be.  The ServiceID mechanism allows us to have multiple types of services offered from the same server on the
        /// same port.
        /// </summary>
        public int ServiceID
        {
            get { return m_ServiceID; }
            set { m_ServiceID = value; }

        }

        protected byte[] m_ConnectionKey = new byte[0];
        /// <summary>
        /// This is the symmetrical Rijndael key that was negotiated during the connection handshake.  This is what will be used anytime a packet
        /// needs to be encrypted or decrypted with this connection
        /// </summary>
        public byte[] ConnectionKey
        {
            get
            {
                return m_ConnectionKey;
            }
            set
            {
                m_ConnectionKey = value;
            }
        }

        private byte[] m_RemoteRsaKey;

        /// <summary>
        /// The remote's RSA key, used primarily to sign UDP packets
        /// </summary>
        public byte[] RemoteRsaKey
        {
            get { return m_RemoteRsaKey; }
            set { m_RemoteRsaKey = value; }
        }

        private static bool m_IsInitialized = false;
        /// <summary>
        /// Gets called before any network communication on the connection are performed.
        /// </summary>
        #endregion

        protected virtual void OnInitialize()
        {
            if (!m_IsInitialized)
            {
                RegisterPacketCreationDelegate((int)PacketType.PacketGenericMessage, delegate { return new PacketGenericMessage(); });
                RegisterPacketCreationDelegate((int)PacketType.LineSecured, delegate { return new PacketLineSecured(); });
                RegisterPacketCreationDelegate((int)PacketType.PacketRijndaelExchangeRequest, delegate { return new PacketRijndaelExchangeRequest(); });
                RegisterPacketCreationDelegate((int)PacketType.PacketGameServerAccessGranted, delegate { return new PacketGameServerTransferResult(); });
                RegisterPacketCreationDelegate((int)PacketType.LoginRequest, delegate { return new PacketLoginRequest(); });
                RegisterPacketCreationDelegate((int)PacketType.LoginResult, delegate { return new PacketLoginResult(); });
                RegisterPacketCreationDelegate((int)PacketType.Null, delegate { return new PacketNull(); });
                RegisterPacketCreationDelegate((int)PacketType.RequestHandoffToServer, delegate { return new PacketRequestHandoffToServerCluster(); });
                RegisterPacketCreationDelegate((int)PacketType.RijndaelExchange, delegate { return new PacketRijndaelExchange(); });
                RegisterPacketCreationDelegate((int)PacketType.PacketStream, delegate { return new PacketStream(); });
                RegisterPacketCreationDelegate((int)PacketType.CharacterListing, delegate { return new PacketCharacterListing(); });
                RegisterPacketCreationDelegate((int)PacketType.GenericReply, delegate { return new PacketReply(); });
                RegisterPacketCreationDelegate((int)PacketType.ACK, delegate { return new PacketACK(); });
                RegisterPacketCreationDelegate((int)PacketType.NATInfo, delegate { return new PacketNATInfo(); });
                RegisterPacketCreationDelegate((int)PacketType.ClockSync, delegate { return new PacketClockSync(); });
                m_IsInitialized = true;
            }

            RegisterPacketHandler((int)PacketType.ACK, OnPacketACK);
            RegisterPacketHandler((int)PacketType.ClockSync, OnClockSync);
            RegisterPacketHandler((int)PacketType.PacketStream, ProcessStreamPacket);
            RegisterPacketHandler((int)PacketType.GenericReply, OnPacketGenericReply);
        }

        #region Packet handling registers

        /// <summary>
        /// Retrieves the multi-cast delegate for a particular packet type
        /// </summary>
        /// <param name="packetType"></param>
        /// <returns></returns>
        public Action<INetworkConnection, Packet> GetPacketHandlerDelegate(int packetType, int packetSubType)
        {
            return m_PacketHandlerMap.GetHandlerDelegate(packetType, packetSubType);
        }

        /// <summary>
        /// Packets contain a PType (packet type) ID in the header.  Use this method to register packet handlers.  Multiple handlers can be registered
        /// for the same packet type.
        /// <para>Example Usage: RegisterPacketHandler((int)PacketType.ServerGreeting, OnMyPacketHandler);</para>
        /// <para>, where OnMyPacketHandler is a method with a signature of: void MethodName(NetworkConnection con, Packet p)</para>
        /// </summary>
        /// <param name="packetType">the packet type ID to register</param>
        /// <returns>false, if @packetType ID is already registered for another delegate</returns>
        public void RegisterPacketHandler(int packetType, Action<INetworkConnection, Packet> handlerMethod)
        {
            RegisterPacketHandler(packetType, 0, handlerMethod);
        }

        /// <summary>
        /// Packets contain a PType (packet type) ID in the header.  Use this method to register packet handlers.  Multiple handlers can be registered
        /// for the same packet type.
        /// <para>Example Usage: RegisterPacketHandler((int)PacketType.ServerGreeting, OnMyPacketHandler);</para>
        /// <para>, where OnMyPacketHandler is a method with a signature of: void MethodName(NetworkConnection con, Packet p)</para>
        /// </summary>
        /// <param name="packetType">the packet type ID to register</param>
        /// <returns>false, if @packetType ID is already registered for another delegate</returns>
        public void RegisterPacketHandler(int packetType, int packetSubType, Action<INetworkConnection, Packet> handlerMethod)
        {
//            Log.LogMsg("Registering packet handler [" + packetType.ToString() + ", sub type " + packetSubType.ToString() + " for " + this.GetType().ToString());
            m_PacketHandlerMap.RegisterHandler(packetType, packetSubType, handlerMethod);
        }

        /// <summary>
        /// Removes a previously registered delegate from the packet handler map
        /// </summary>
        /// <param name="packetType">the packet type to remove the handler for</param>
        /// <param name="handlerMethod">the method to unregister</param>
        public void UnregisterPacketHandler(int packetType, int packetSubType, Action<INetworkConnection, Packet> handlerMethod)
        {
            m_PacketHandlerMap.UnregisterHandler(packetType, packetSubType, handlerMethod);
        }

        /// <summary>
        /// Packets contain a PType (packet type) ID in the header.  Based on this ID, incoming (and sometimes outgoing) the appropriate packet objects
        /// are instantiated.  New packet type IDs must be registered with this method before the system knows about them.  A quick way to do this is via
        /// this method as such:
        /// <para>Example Usage: RegisterPacketCreationDelegate((int)PacketType.ServerGreeting, delegate { return new PacketServerGreeting(); });</para>
        /// </summary>
        /// <returns>false, if @packetType ID is already registered for another delegate</returns>
        public static bool RegisterPacketCreationDelegate(int packetType, PacketCreationDelegate packetContructionMethod)
        {
            return RegisterPacketCreationDelegate(packetType, 0, packetContructionMethod);
        }

        /// <summary>
        /// Packets contain a PType (packet type) ID in the header.  Based on this ID, incoming (and sometimes outgoing) the appropriate packet objects
        /// are instantiated.  New packet type IDs must be registered with this method before the system knows about them.  A quick way to do this is via
        /// this method as such:
        /// <para>Example Usage: RegisterPacketCreationDelegate((int)PacketType.ServerGreeting, delegate { return new PacketServerGreeting(); });</para>
        /// </summary>
        /// <returns>false, if @packetType ID is already registered for another delegate</returns>
        public static bool RegisterPacketCreationDelegate(int packetType, int packetSubType, PacketCreationDelegate packetContructionMethod)
        {
            return PacketCreationMap.RegisterHandler(packetType, packetSubType, packetContructionMethod);
        }

        /// <summary>
        /// Returns the delegate that will create the appropriate packet type for @packetType
        /// </summary>
        public static PacketCreationDelegate GetPacketCreationDelegate(int packetType)
        {
            return GetPacketCreationDelegate(packetType, 0);
        }

        /// <summary>
        /// Returns the delegate that will create the appropriate packet type for @packetType
        /// </summary>
        public static PacketCreationDelegate GetPacketCreationDelegate(int packetType, int packetSubType)
        {
            return PacketCreationMap.GetHandlerDelegate(packetType, packetSubType);
        }

        /// <summary>
        /// Retrieves the multi-cast delegate for a particular generic reply packet 
        /// </summary>
        /// <param name="genericPacketType"></param>
        /// <returns></returns>
        public Action<INetworkConnection, Packet> GetStandardPacketReplyHandlerDelegate(int repliedToPacketType)
        {
            Action<INetworkConnection, Packet> handler = StandardReplyHandlerMap.GetHandlerDelegate(repliedToPacketType);
            return handler;
        }

        /// <summary>
        /// Retrieves the multi-cast delegate for a particular generic reply packet 
        /// </summary>
        /// <param name="genericPacketType"></param>
        /// <returns></returns>
        public Action<INetworkConnection, Packet> GetStandardPacketReplyHandlerDelegate(int repliedToPacketType, int repliedToPacketSubType)
        {
            Action<INetworkConnection, Packet> handler = StandardReplyHandlerMap.GetHandlerDelegate(repliedToPacketType, repliedToPacketSubType);
            return handler;
        }

        /// <summary>
        /// Causes the handler method to be invoked when a particular packet of type and no sub type was responded to
        /// </summary>
        /// <param name="repliedToPacketType"></param>
        /// <param name="repliedToPacketSubType"></param>
        /// <param name="handlerMethod"></param>
        public void RegisterStandardPacketReplyHandler(int repliedToPacketType, Action<INetworkConnection, Packet> handlerMethod)
        {
            RegisterStandardPacketReplyHandler(repliedToPacketType, 0, handlerMethod);
        }

        /// <summary>
        /// Causes the handler method to be invoked when a particular packet of type and subtype was responded to
        /// </summary>
        /// <param name="repliedToPacketType"></param>
        /// <param name="repliedToPacketSubType"></param>
        /// <param name="handlerMethod"></param>
        public void RegisterStandardPacketReplyHandler(int repliedToPacketType, int repliedToPacketSubType, Action<INetworkConnection, Packet> handlerMethod)
        {
            StandardReplyHandlerMap.RegisterHandler(repliedToPacketType, repliedToPacketSubType, handlerMethod);
        }

        /// <summary>
        /// Removes a previously registered delegate from the generic packet handler map
        /// </summary>
        /// <param name="packetType">the generic packet type to remove the handler for</param>
        /// <param name="handlerMethod">the method to unregister</param>
        public void UnregisterStandardPacketHandler(int repliedToPacketType, Action<INetworkConnection, Packet> handlerMethod)
        {
            UnregisterStandardPacketHandler(repliedToPacketType, 0, handlerMethod);
        }

        /// <summary>
        /// Removes a previously registered delegate from the generic packet handler map
        /// </summary>
        /// <param name="packetType">the generic packet type to remove the handler for</param>
        /// <param name="handlerMethod">the method to unregister</param>
        public void UnregisterStandardPacketHandler(int repliedToPacketType, int repliedToPacketSubType, Action<INetworkConnection, Packet> handlerMethod)
        {
            StandardReplyHandlerMap.UnregisterHandler(repliedToPacketType, repliedToPacketSubType, handlerMethod);
        }


        #endregion

        #region CreatePacket

        /// <summary>
        ///  When packets arrive across the wire, they must be
        /// instantiated based based on their PacketType which is just an integer.
        /// This helper method does that.
        /// </summary>
        /// <param name="header"></param>
        /// <returns></returns>
        public Packet CreatePacket(int type, int subType, bool encrypt, bool compress)
        {
            Packet p = null;
            try
            {
                PacketCreationDelegate create = PacketCreationMap.GetHandlerDelegate(type, subType);
                if (create == null)
                {
                    throw new ArgumentOutOfRangeException("Packet creation handler for packet type " + type.ToString() + " was not registered for " + GetType().ToString() + ", sub-type " + subType);
                }

                p = create();
                p.IsCompressed = compress;
                p.IsEncrypted = encrypt;
                p.PacketTypeID = type;
                p.PacketSubTypeID = subType;
            }
            catch (Exception e)
            {
                Log.LogMsg("Failed to instantiate packet. " + e.Message);
                KillConnection("Failed to instantiate packet. " + e.Message);
            }
            return p;
        }

        private Packet CreatePacket(int type, int subType, PacketFlags flags, byte[] data)
        {
            Packet p = CreatePacket(type, subType, false, false);

            if (p != null)
            {
                p.Flags = flags;
                p.PacketTypeID = type;
                p.PacketSubTypeID = subType;
                //Log.LogMsg("Attempting to deserialize " + (p.IsUDP? "UDP" : "TCP") + " packet "+ p.PacketTypeID.ToString());
                p.DeSerialize(data, new Pointer());
                //Log.LogMsg("Deserialized " + (p.IsUDP? "UDP" : "TCP") + " packet " + p.GetType().ToString());
            }
            return p;
        }
        #endregion

        #region Virtual & Override

        /// <summary>
        /// Override to catch socket killed occurance.  Be sure to call this base method, unless you do not want the SocketKilled event to fire
        /// </summary>
        /// <param name="msg"></param>
        protected virtual void OnSocketKilled(string msg)
        {
            FireSocketKilled(this, msg);
        }

        /// <summary>
        /// Override to implement packet data encryption / compression
        /// </summary>
        /// <param name="body">the raw data that would need to be encrypted and/or compressed</param>
        /// <returns>The encoded data</returns>
        protected virtual byte[] OnBeforeSendPacket(byte[] body, bool encrypt, bool compress, Pointer bodyPointer)
        {
            return body;
        }

        /// <summary>
        /// Override this method to process raw packet body data when it arrives.  Use this opportunity to decompress and/or decrypt data, if necessary
        /// </summary>
        /// <param name="data">the raw data - maybe be compressed and/or encrypted using the previously negotiated Rijndael key</param>
        /// <returns>the clean, plain data after any applicable decompression and decryption</returns>
        protected virtual byte[] OnPacketDataArrived(byte[] data, int dataStart, int dataLen, PacketFlags flags)
        {
            byte[] body;
            if ((flags & PacketFlags.IsEncrypted) != 0 && m_ConnectionKey.Length > 0)
            {
                body = CryptoManager.RijDecrypt(data, dataStart, dataLen, m_ConnectionKey);
            }
            else
            {
                if (dataLen == data.Length)
                {
                    return data;
                }
                body = new byte[dataLen];
                Util.Copy(data, dataStart, body, 0, dataLen);
            }

            if ((flags & PacketFlags.IsCompressed) != 0 & data.Length > 0)
            {
                body = Compression.DecompressData(body, dataStart);
            }

            return body;
        }

        /// <summary>
        /// Fires before a packet is processed. Return false from this method to prevent packet handlers from being called.
        /// This is the appropriate place to check authorization, stop timeout timers, etc, etc.
        /// Base method handles repeat processing safeguards and sending ACKnowledgement for requesting parties as well
        /// as checking the message signature in the case of signed UDP packets.
        /// If you override NetworkConnection.HandlePacket and do not end up calling base.HandlePacket(), you need to call 
        /// this method manually after you have completed processing your packet.
        /// </summary>
        /// <param name="p">the packet in question</param>
        /// <returns>return false from this method to prevent handlers from being called</returns>
        public virtual bool OnBeforePacketProcessed(Packet p)
        {
            bool process = true;
            // check signature, if necessary - we dont do signatures for TCP, it's already handled in the protocol
            if (p.IsUDP)
            {
                if (!m_CanSendUDP)
                {
                    p.NeedsDeliveryAck = false;
                }
            }

            if (PreventRepeatedPacketProcessing)
            {
                if (0 == Interlocked.Exchange(ref m_UpdatingPacketLog, 1)) // asynchronous networking requires us to lock this resource so only one thread can update the log at a time
                {
                    if (m_LastFewPacketsAnswered.IndexOf(p.PacketID) > -1)
                    {
                        // don't process the packet again
                        process = false;
                    }
                    else
                    {
                        m_LastFewPacketsAnswered.Add(p.PacketID);
                        if (m_LastFewPacketsAnswered.Count > 25)
                        {
                            m_LastFewPacketsAnswered.RemoveAt(0);
                        }
                    }

                    //Release the lock
                    Interlocked.Exchange(ref m_UpdatingPacketLog, 0);
                }
            }

            // send the ack, if requested
            if (p.NeedsDeliveryAck)
            {
                SendPacketAck(p);
            }

            if (process && !p.IsUDP)
            {
                m_LastTCPPacketIdProcessed = p.PacketID;
            }
            return process;
        }

        /// <summary>
        /// Fires after all packet handlers have processed the packet.  This is the appropriate place to restart time out timers, etc
        /// Base method handles duplicate request safeguards and sending of the reply packet.  If you override NetworkConnection.HandlePacket and
        /// do not end up calling base.HandlePacket(), you need to call this method manually after you have completed processing your packet.
        /// </summary>
        /// <param name="p">the packet that was just processed</param>
        public virtual void OnAfterPacketProcessed(Packet p)
        {
            if (PreventRepeatedPacketProcessing)
            {
                if (0 == Interlocked.Exchange(ref m_UpdatingPacketLog, 1)) // asynchronous networking requires us to lock this resource so only one thread can update the log at a time
                {
                    if (p.ReplyPacket != null && p.ReplyPacket.ReplyCode == ReplyType.AuthorizationTicketExpired) // NoAuth reply means the packet wasn't processed.  Remove it from the processed list.
                    {
                        // it was not authorized, hence not processed.  remove it from the recently processed list
                        m_LastFewPacketsAnswered.Remove(p.PacketID);
                    }

                    //Release the lock
                    Interlocked.Exchange(ref m_UpdatingPacketLog, 0);
                }
            }

            if (p.ReplyPacket != null)
            {
                if (p.NeedsReply)
                {
                    if (Transit != null)
                    {
                        Transit.Send(p.ReplyPacket);
                    }
                }

                if (p.ReplyPacket.IsCritical && p.ReplyPacket.ReplyCode != ReplyType.OK)
                {
                    KillConnection("Critical packet failed. " + p.ReplyPacket.ReplyMessage);
                }
            }
        }

        /// <summary>
        /// Gets called after a packet has been received, reconstructed, decrypted and decompressed (as applicable) and is thus ready for
        /// processing by the connection.  Override this method to handle a packet having been received. 	
        /// </summary>
        /// <param name="packet">the network packet which has arrived</param>
        private void OnPacketReceived(Packet msg)
        {
            if (!IsAlive)
            {
                return;
            }

            Action<INetworkConnection, Packet> handler = null;
            if (msg.PacketTypeID == (int)PacketType.GenericReply)
            {
                handler = GetPacketHandlerDelegate(msg.PacketTypeID, 0);
            }
            else if (msg.PacketTypeID == (int)PacketType.PacketStream)
            {
                // call the base stream handler handler
                handler = GetPacketHandlerDelegate(msg.PacketTypeID, 0);
            }
            else
            {
                handler = GetPacketHandlerDelegate(msg.PacketTypeID, msg.PacketSubTypeID);
            }

            if (handler != null)
            {
                try
                {
                    handler(this, msg);
                }
                catch (Exception e)
                {
                    Log.LogMsg("Exception thrown whilst processing packet type " + msg.PacketTypeID.ToString() + ", sub-type " + msg.PacketSubTypeID + ". Object = " +this.GetType().ToString() + ", Message: " + e.Message + ". Stack:\r\n " + e.StackTrace);
                }

                OnAfterPacketProcessed(msg);
                return;
            }

            KillConnection(this.GetType().ToString() + " did not have a registered packet handler for packet " + msg.PacketTypeID.ToString() + ", sub-type " + msg.PacketSubTypeID);
            //throw new NotImplementedException(this.GetType().ToString() + " did not implement packet handler for packet " + msg.GetType().ToString() + ".");
        }

        #endregion

                /// <summary>
        /// Shuts down the socket and cleans up resources. Also calls OnSocketKilled and fires SocketKilled event. Once a connection has been killed
        /// by whatever means, it can't be reused.
        /// </summary>
        public void KillConnection(string msg)
        {
            StackTrace st = new StackTrace();

            //Log.LogMsg("DC ST: " + st.ToString());
            KillConnection(msg, true);
        }


        private string m_DisconnectMessage = "";
        /// <summary>
        /// Shuts down the socket and cleans up resources. Also calls OnSocketKilled and fires SocketKilled event. Once a connection has been killed
        /// by whatever means, it can't be reused.
        /// </summary>
        public void KillConnection(string msg, bool allowPendingPacketsToSend)
        {
            m_DisconnectMessage = msg;            
            if (0 != Interlocked.Exchange(ref m_ShuttingDown, 1))
            {
                return;
            }

            if (!allowPendingPacketsToSend)
            {
                PerformDisconnection(msg);
                return;
            }

            if (Transit != null)
            {
                if (Transit.HasQueuedPackets)
                {
                    SetDisconnectTimer(500);
                    return;
                }

                PerformDisconnection(msg);
            }
        }


        private void TryDisconnect(object state)
        {
            if (Transit != null)
            {
                if (Transit.HasQueuedPackets)
                {
                    SetDisconnectTimer(500);
                    return;
                }                
            }

            PerformDisconnection(m_DisconnectMessage);
        }

        private void PerformDisconnection(string msg)
        {
            try
            {
                OnSocketKilled(msg);

                if (Transit != null)
                {
                    Transit.BeforeShutdown();
                }
            }
            catch (Exception exp)
            {
                int x = 0;
            }
            finally
            {
                if (MyTCPSocket != null)
                {
                    Log.LogMsg("Closing TCP Socket.");
                   
                    
                    if (MyTCPSocket.Connected)
                    {
                        MyTCPSocket.Shutdown(SocketShutdown.Both);
                    }
                   
                    MyTCPSocket.Close();
                }

                if (MyUDPSocket != null)
                {
                    Log.LogMsg("Closing UDP Send Socket.");
                    // MyUDPSocket.Shutdown(SocketShutdown.Both);
                    MyUDPSocket.Close();
                }

                MyTCPSocket = null;
                MyUDPSocket = null;

                if (Transit != null)
                {
                    Transit.AfterShutdown();
                }

                //ShuttingDown = false;
                Log.LogMsg("KillConnection -->>----> " + msg);
                //Log.LogMsg("==> " + SocketAsyncEventArgsCache.ReportPoolStatus());     

                Dispose();
            }
        }

        #region Timer Util

        private System.Threading.Timer m_Timer;

        private void SetDisconnectTimer(int ms)
        {
            if (ms < 1)
            {
                CancelTimer();
                return;
            }

            if (m_Timer == null)
            {
                m_Timer = new System.Threading.Timer(new TimerCallback(TryDisconnect), null, ms, Timeout.Infinite);
            }
            else
            {
                CancelTimer();
                m_Timer.Change(ms, Timeout.Infinite);
            }
        }

        private void CancelTimer()
        {
            if (m_Timer == null)
            {
                return;
            }
            m_Timer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
        }

        #endregion

        /// <summary>
        /// Gets called when we receive a clock synchronization packet
        /// </summary>
        /// <param name="sender">the connection sending the sync packet</param>
        /// <param name="msg">the sync data</param>
        private void OnClockSync(INetworkConnection sender, Packet msg)
        {
            OnClockSync(msg as PacketClockSync);
        }

        /// <summary>
        /// Fires when we receive a clock synchronization packet
        /// </summary>
        /// <param name="sender">the connection sending the sync packet</param>
        /// <param name="msg">the sync data</param>
        protected virtual void OnClockSync(PacketClockSync msg)
        {
        }

        /// <summary>
        /// Send out packet failure messages to listeners, if application
        /// </summary>
        private void OnPacketACK(INetworkConnection con, Packet msg)
        {
            OnPacketReceiptACK(msg as PacketACK);
        }

        protected virtual void OnPacketAuthFailed(PacketReply replyPacket)
        {
        }

        /// <summary>
        /// Send out packet reply messages to listeners, if application
        /// </summary>
        private void OnPacketGenericReply(INetworkConnection con, Packet msg)
        {
            PacketReply rep = msg as PacketReply;
            if (rep.ReplyCode == ReplyType.AuthorizationTicketExpired)
            {
                OnPacketAuthFailed(rep);
            }

            Action<INetworkConnection, Packet> handler = GetStandardPacketReplyHandlerDelegate(rep.ReplyPacketType, rep.ReplyPacketSubType);
            if (handler != null)
            {
                handler(this, rep);
            }
        }

#if SILVERLIGHT
        /// <summary>
        /// Gets called when a filestream has been completed successfully. If you return True from this method, the file will be deleted
        /// from the temp location when the method has completed.  The default return value is True.
        /// </summary>
        /// <param name="file">the stream to the file</param>
        /// <param name="totalFileLengthBytes">total length of the file that was downloaded</param>
        /// <param name="subType">arbitrary sub type argument, sent by the remote</param>
        /// <param name="arg">arbitrary argument sent by the remote, if any</param>
        protected virtual bool OnFileStreamComplete(Stream file, string fileName, long totalFileLengthBytes, int subType, string arg)
        {
            return true;
        }
#else
        /// <summary>
        /// Gets called when a filestream has been completed successfully. If you return True from this method, the file will be deleted
        /// from the temp location when the method has completed.  The default return value is True.
        /// </summary>
        /// <param name="file">the stream to the file</param>
        /// <param name="totalFileLengthBytes">total length of the file that was downloaded</param>
        /// <param name="subType">sub type argument, sent by the remote</param>
        /// <param name="arg">arbitrary argument sent by the remote, if any</param>
        protected virtual bool OnFileStreamComplete(string fileName, long totalFileLengthBytes, int subType, string arg)
        {
            return true;
        }
#endif

        protected virtual void OnFileStreamProgress(string path, long currentBytesDownloaded, long totalBytesToDownload)
        {
        }

        private long m_CurrentDLTotalLength = -1;
        private long m_CurrentDLCurrentDown = -1;
        private string m_CurrentDLFilename = "";
        private string m_CurrentDLDiskname = "";
        private Stream m_CurrentFileStream = null;

        protected virtual void ProcessStreamPacket(INetworkConnection con, Packet packet)
        {
            PacketStream packetStream = packet as PacketStream;
            try
            {
#if !SILVERLIGHT
                string directory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                if (!Directory.Exists(Path.Combine(directory, "TEMP")))
                {
                    Directory.CreateDirectory(Path.Combine(directory, "TEMP"));
                }

                string filename = Path.Combine(directory, "TEMP");
                filename = Path.Combine(filename, packetStream.Description);
#else
                string filename = packetStream.Description;
#endif
                if (m_CurrentFileStream == null)
                {
                    m_CurrentDLFilename = filename;
#if !SILVERLIGHT
                    m_CurrentFileStream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.Write);
#else
                    m_CurrentFileStream = new MemoryStream();
#endif
                }

                if (m_CurrentDLFilename != filename)
                {
                    m_CurrentFileStream.Close();
                    m_CurrentFileStream.Dispose();
                    try
                    {
#if !SILVERLIGHT
                        File.Delete(m_CurrentDLFilename);
#else
                        m_CurrentFileStream.Close();
                        m_CurrentFileStream.Dispose();
#endif
                    }
                    catch { }
#if !SILVERLIGHT

                    m_CurrentFileStream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.Write);
#else
                     m_CurrentFileStream = new MemoryStream();
#endif
                }

                m_CurrentFileStream.Write(packetStream.Buffer, 0, packetStream.Buffer.Length);

                if (packetStream.Final)
                {
                    m_CurrentDLCurrentDown = -1;
                    m_CurrentDLTotalLength = -1;
                    m_CurrentDLFilename = "";

                    string fileName = m_CurrentDLFilename;
#if !SILVERLIGHT
                    m_CurrentFileStream.Close();
                    m_CurrentFileStream.Dispose();
#else
                    m_CurrentFileStream.Seek(0, SeekOrigin.Begin);
#endif

#if SILVERLIGHT
                    if (OnFileStreamComplete(m_CurrentFileStream, filename, packetStream.TotalLength, packetStream.PacketSubTypeID, packetStream.Arg))
                    {
                        try
                        {
                            m_CurrentFileStream.Close();
                            m_CurrentFileStream.Dispose();                            
                        }
                        catch { }
                    }
#else
                    if (OnFileStreamComplete(filename, packetStream.TotalLength, packetStream.PacketSubTypeID, packetStream.Arg))
                    {
                        try
                        {
                            File.Delete(filename);
                        }
                        catch { }
                    }
#endif
                }
                else
                {
                    m_CurrentDLCurrentDown = m_CurrentFileStream.Position;
                    m_CurrentDLTotalLength = packetStream.TotalLength;
                    m_CurrentDLFilename = filename;
                    OnFileStreamProgress(filename, m_CurrentDLCurrentDown, m_CurrentDLTotalLength);
                }
            }
            catch
            {
            }
        }

        protected virtual void OnPacketReceiptACK(PacketACK msg)
        {
            Transit.OnPacketReceiptACK(msg);
        }

        /// <summary>
        /// The endpoint we are connected to
        /// </summary>
        public EndPoint RemoteEndPoint
        {
            get
            {
                if (MyTCPSocket == null || !MyTCPSocket.Connected || MyTCPSocket.RemoteEndPoint == null)
                {
                    return new IPEndPoint(0, 0);
                }
                try
                {
                    return MyTCPSocket.RemoteEndPoint;
                }
                catch { }

                return new IPEndPoint(0, 0);
            }
        }

        #region Send Generic Message Methods

        /// <summary>
        /// Sends a generic command with parameters to the remote endpoint
        /// </summary>
        /// <param name="msgType">the generic message id, used by the remote endpoint to determine what kind of message this is</param>
        /// <param name="text">any kind of text to send along with the message</param>
        /// <param name="parms">a property bag of misc information to send along with the message</param>
        /// <param name="encrypt">true, if the message should be encrypted</param>
        /// <returns>true, if the message was sent</returns>
        public bool SendGenericMessage(int msgType, string text, PropertyBag parms, bool encrypt)
        {
            PacketGenericMessage p = (PacketGenericMessage)CreatePacket((int)PacketType.PacketGenericMessage, 0, encrypt, false);
            p.TextMessage = text;
            p.PacketSubTypeID = msgType;

            if (parms != null)
            {
                p.Parms = parms;
            }
            else
            {
                p.Parms = new PropertyBag();
            }
            return Transit.Send(p) == 1;
        }

        /// <summary>
        /// Sends a generic command with parameters to the remote endpoint
        /// </summary>
        /// <param name="msgType">the generic message id, used by the remote endpoint to determine what kind of message this is</param>
        /// <param name="parms">a property bag of misc information to send along with the message</param>
        /// <param name="encrypt">true, if the message should be encrypted</param>
        /// <returns>true, if the message was sent</returns>
        public bool SendGenericMessage(int msgType, PropertyBag parms, bool encrypt)
        {
            return SendGenericMessage(msgType, "", parms, encrypt);
        }

        /// <summary>
        /// Sends a generic command with parameters to the remote endpoint
        /// </summary>
        /// <param name="msgType">the generic message id, used by the remote endpoint to determine what kind of message this is</param>
        /// /// <param name="text">any kind of text to send along with the message</param>
        /// <param name="encrypt">true, if the message should be encrypted</param>
        /// <returns>true, if the message was sent</returns>
        public bool SendGenericMessage(int msgType, string text, bool encrypt)
        {
            return SendGenericMessage(msgType, text, null, encrypt);
        }

        /// <summary>
        /// Sends a generic command with parameters to the remote endpoint
        /// </summary>
        /// <param name="msgType">the generic message id, used by the remote endpoint to determine what kind of message this is</param>
        /// <param name="encrypt">true, if the message should be encrypted</param>
        /// <returns>true, if the message was sent</returns>
        public bool SendGenericMessage(int msgType, bool encrypt)
        {
            return SendGenericMessage(msgType, "", encrypt);
        }

        /// <summary>
        /// Sends a generic unencrypted command with parameters to the remote endpoint
        /// </summary>
        /// <param name="msgType">the generic message id, used by the remote endpoint to determine what kind of message this is</param>
        /// <returns>true, if the message was sent</returns>
        public bool SendGenericMessage(int msgType)
        {
            return SendGenericMessage(msgType, "", false);
        }

        #endregion


        #region Send Packet Reply Methods

        /// <summary>
        /// Generates a PacketReply based on the packet that is generating this request.
        /// </summary>
        /// <param name="request">the packet that this request is generated for</param>
        /// <param name="replyCode">result of the original reply</param>
        /// <param name="msg">any textual message, or nont at all</param>
        /// <returns></returns>
        public PacketReply CreateStandardReply(Packet request, ReplyType replyCode, string msg)
        {
            if (msg == null)
            {
                msg = "";
            }
            PacketReply p = (PacketReply)CreatePacket((int)PacketType.GenericReply, 0, request.IsEncrypted, request.IsCompressed);
            p.ReplyPacketID = request.PacketID;
            p.ReplyCode = replyCode;
            p.ReplyPacketType = request.PacketTypeID;
            p.ReplyPacketSubType = request.PacketSubTypeID;
            p.ReplyMessage = msg;
            p.IsUDP = request.IsUDP;

            return p;
        }

        public bool SendPacketAck(Packet packet)
        {
            bool rslt = true;

            PacketACK p = (PacketACK)CreatePacket((int)PacketType.ACK, 0, false, false);
            p.ReplyPacketID = packet.PacketID;
            p.ReplyCode = ReplyType.OK;
            p.ReplyPacketType = packet.PacketTypeID;
            p.ReplyPacketSubType = packet.PacketSubTypeID;
            p.IsUDP = packet.IsUDP;

            Transit.Send(p);
            return rslt;
        }

        public bool SendPacketAck(PacketGenericMessage packet)
        {
            bool rslt = true;

            PacketACK p = (PacketACK)CreatePacket((int)PacketType.ACK, 0, false, false);
            p.ReplyPacketID = packet.PacketID;
            p.ReplyCode = ReplyType.OK;
            p.ReplyPacketType = packet.PacketTypeID;
            p.ReplyPacketSubType = packet.PacketSubTypeID;
            p.IsUDP = packet.IsUDP;

            Transit.Send(p);
            return rslt;
        }

        #endregion

        /// <summary>
        /// Sends a binary file stream across the connection.
        /// </summary>
        /// <param name="fi">The FileInfo object for the file in question</param>
        /// <param name="arg">an arbitrary string argument that will be sent as the "Arg" property of the PacketStream packet</param>
        /// <param name="subType">Any subtype ID you want to pass to the remote end.  This allows the remote end to customize its response to a file transfer depending on its subType</param>
        public bool SendFileStream(FileInfo fi, string arg, int subType)
        {
            FileStream fs = null;
            try
            {
                //Log1.Logger("Networking").Info("Sending file " + fi.Name + " to " + this.RemoteEndPoint.ToString());
                DateTime start = DateTime.Now;
                fs = fi.OpenRead();
                int num = 0;
                while (fs.Position != fs.Length)
                {
                    if (!IsAlive)
                    {
                        //Log1.Logger("Networking").Info("File transfer aborted.");
                        return false;
                    }
                    num++;
                    int size = 8196;
                    if (fs.Length - fs.Position < size)
                    {
                        size = (int)(fs.Length - fs.Position);
                    }

                    PacketStream p = new PacketStream(size);
                    p.PacketTypeID = (int)PacketType.PacketStream;
                    p.PacketSubTypeID = subType;
                    p.IsEncrypted = false;
                    p.IsCompressed = false;
                        
                    p.Description = fi.Name;
                    p.Arg = arg;
                    p.Buffer = new byte[size];
                    p.TotalLength = fi.Length;
                    fs.Read(p.Buffer, 0, size);

                    if (num == 1)
                    {
                        p.Initial = true;
                    }

                    if (fs.Position == fs.Length)
                    {
                        p.NeedsDeliveryAck = true;
                        p.Final = true;
                    }

                    Transit.Send(p);
                    OnFileStreamProgress("", fs.Position, fs.Length);

                    if (p.Final)
                    {
#if SILVERLIGHT
                        OnFileStreamComplete(fs, "", fs.Length, 0, "");
#else
                        OnFileStreamComplete(fi.FullName, fs.Length, 0, "");
#endif
                    }
                }

                DateTime now = DateTime.Now;
                TimeSpan duration = now - start;
                string msg = string.Format("Sent {0} MB file {1} to {2} in {3}.", new object[] { Util.ConvertBytesToMegabytes(fi.Length).ToString("N"), "file", RemoteEndPoint.ToString(),/* duration.ToString("hh\\:mm\\:ss\\:ff")*/ duration.TotalSeconds.ToString() + " Minutes" });
                //Log1.Logger("Networking").Info(msg);
            }
            catch (Exception e)
            {
                //Log1.Logger("Networking").Error("Failed to send file stream " + fi.FullName, e);
                return false;
            }
            finally
            {
                if (fs != null)
                {
                    fs.Close();
                    fs.Dispose();
                }
            }

            return true;
        }

        /// <summary>
        /// Sends a binary file stream across the connection.
        /// </summary>
        /// <param name="fi">The FileInfo object for the file in question</param>
        /// <param name="arg">an arbitrary string argument that will be sent as the "Arg" property of the PacketStream packet</param>
        public bool SendFileStream(FileInfo fi, string arg)
        {
            return SendFileStream(fi, arg, 0);
        }

        public int Send(Packet p)
        {
            if (Transit != null)
            {
                return Transit.Send(p);
            }
            return 0;
        }

        public int Send(byte[] data, PacketFlags flags)
        {
            if (Transit != null)
            {
                return Transit.Send(data, flags);
            }
            return 0;
        }

        /// <summary>
        /// Called by UDP listeners when they receive an UDP packet
        /// </summary>
        public void SetLastUDPACKReceived(DateTime when)
        {
            if (Transit != null)
            {
                Transit.LastUDPACKReceived = when;
            }
        }

        private long m_Disposed = 0;

        public virtual void Dispose()
        {
            Log.LogMsg("Disposing network connection.");

            if (0 != Interlocked.Exchange(ref m_Disposed, 1))
            {
                return;
            }

#if !SILVERLIGHT
            if (Transit != null && Transit is SimplexAsyncTransitStrategy)
            {
                ((SimplexAsyncTransitStrategy)Transit).StopNetworkingPump();
            }
#endif

            Log.LogMsg("Network connection Disposed.");

            // Indicate that the instance has been disposed.
            Transit  = null;
        }



    }
}
