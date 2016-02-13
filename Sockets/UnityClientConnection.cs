using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Threading;

namespace Shared
{
    public class UnityClientConnection : INetworkConnection, IClientConnection
    {
		public UnityClientConnection(bool isBlocking) : this()
		{
		}
		public UnityClientConnection()
		{
			OnInitialize();
		}
        #region NetworkConnection

        #region Data
        private static bool m_IsInitialized = false;
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
        /// <summary>
        /// If the connection is in the process of shutting down.  This state generally only persists for a few miliseconds.
        /// </summary>
        public bool ShuttingDown
        {
            get
            {
                return m_ShuttingDown == 1;
            }
        }
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
        private Guid m_UID = Guid.Empty;
        protected Queue<NetQItem> m_SendQueue = new Queue<NetQItem>(); // packet send queue
        public DateTime LastUDPACKReceived { get; set; }
        private byte[] m_TCPBuffer = new byte[4096];
        private int m_TCPBytesReceived = 0;
        private SockState m_TCPSockState;
        private SocketError m_LastTCPReceiveStatus = SocketError.NotInitialized;
        private PacketHandlerMap m_PacketHandlerMap = new PacketHandlerMap();
        protected CryptoManager CryptoManager = new CryptoManager();
        private long m_ShuttingDown = 0;
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
        /// History of the last few packets that were processed on this connection. Mostly this is used 
        /// to make sure that if the client transmits a request multiple times, that it doesnt get
        /// processed multiple times
        /// </summary>
        private List<int> m_LastFewPacketsAnswered = new List<int>();
        /// <summary>
        /// Used to lock access to the packet log where we store the last several packets that were processed
        /// </summary>
        private int m_UpdatingPacketLog = 0;
        private bool m_CanSendUDP = false;
        public bool CanSendUDP
        {
            get { return m_CanSendUDP; }
            set { m_CanSendUDP = value; }
        }
        private int m_LastTCPPacketIdProcessed = 0;
        private PacketHandlerMap m_StandardReplyHandlerMap = new PacketHandlerMap();
        /// <summary>
        /// Stores delegates to create packets, based on the packet type 
        /// </summary>
        private PacketHandlerMap StandardReplyHandlerMap
        {
            get { return m_StandardReplyHandlerMap; }
            set { m_StandardReplyHandlerMap = value; }
        }
        /// <summary>
        /// The number of packet acknowledgements we're waiting for
        /// </summary>
        private int m_NumAcksWaitingFor = 0;
        public int NumAcksWaitingFor
        {
            get { return m_NumAcksWaitingFor; }
        }
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
        private byte[] m_RemoteRsaKey;
        /// <summary>
        /// The remote's RSA key, used primarily to sign UDP packets
        /// </summary>
        public byte[] RemoteRsaKey
        {
            get { return m_RemoteRsaKey; }
            set { m_RemoteRsaKey = value; }
        }
        private Socket m_MyTCPSocket;
        public Socket MyTCPSocket
        {
            set
            {
                m_MyTCPSocket = value;
            }
            get
            {
                return m_MyTCPSocket;
            }
        }
        private Socket m_MyUDPSocket;
        public Socket MyUDPSocket
        {
            set
            {
                m_MyUDPSocket = value;
            }
            get
            {
                return m_MyUDPSocket;
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
        public bool BlockingMode
        {
            get
            {
                return true;
            }
            set
            {
                if (!value)
                {
                    Log.LogMsg("Warning: UnityClientConnection only operates in Blocking mode. Changing this value has no effect.");
                }
            }
        }

        #endregion

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

        /// <summary>
        /// Assembles the packet across however many frames it takes
        /// </summary>
        /// <param name="data"></param>
        public void AssembleInboundPacket(SocketAsyncEventArgs args, SockState state)
        {
            throw new NotImplementedException();
        }

        public void AssembleInboundPacket(byte[] buffer, int bytesReceived, SockState state)
        {
            try
            {
                int incomingPointer = 0; // how much of the incoming data have we read

                while (incomingPointer < bytesReceived)
                {
                    if (state.MessageLength == -1) // don't know how long the message is, still need to read the envelope
                    {
                        int leftForEnvelope = SockState.EnvelopeLength - state.PacketBufferPointer.Position;
                        int numToCopy = leftForEnvelope;
                        int leftInBlock = bytesReceived - incomingPointer;
                        if (numToCopy > leftInBlock)
                        {
                            numToCopy = leftInBlock;
                        }

                        Util.Copy(buffer, state.BufferBlockOffset + incomingPointer, state.PacketBuffer, state.PacketBufferPointer.Advance(numToCopy), numToCopy);
                        incomingPointer += numToCopy;

                        if (state.PacketBufferPointer.Position >= SockState.EnvelopeLength)
                        {
                            state.MessageLength = BitConverter.ToInt32(state.PacketBuffer, 0);
                            state.PacketTypeID = BitConverter.ToInt32(state.PacketBuffer, 4);
                            state.PacketSubTypeID = BitConverter.ToInt32(state.PacketBuffer, 8);// state.PacketBuffer[8];
                            state.Flags = (PacketFlags)state.PacketBuffer[12];
                            state.PacketBufferPointer.Reset();
                            state.PacketBuffer = new byte[state.MessageLength];
                            continue;
                        }

                        return;
                    }

                    int bytesNeededToCompleteMessage = state.PacketBuffer.Length - state.PacketBufferPointer.Position;
                    int bytesToRead = bytesNeededToCompleteMessage;
                    if (bytesToRead > bytesReceived - incomingPointer)
                    {
                        bytesToRead = bytesReceived - incomingPointer;
                    }

                    Util.Copy(buffer, state.BufferBlockOffset + incomingPointer, state.PacketBuffer, state.PacketBufferPointer.Advance(bytesToRead), bytesToRead);
                    incomingPointer += bytesToRead;

                    if (state.PacketBufferPointer.Position >= state.MessageLength)
                    {
                        DeserializePacket(state);
                        state.Reset();
                    }
                }
            }
            catch (Exception readExc)
            {
                Log.LogMsg(readExc.Message + ": Shutting down socket.\r\n" + readExc.StackTrace);
                KillConnection("");
            }
        }

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
                PacketCreationDelegate create = NetworkConnection.PacketCreationMap.GetHandlerDelegate(type, subType);
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
            PacketReply p = (PacketReply)CreatePacket((int)PacketType.GenericReply, 0, false, false);
            p.ReplyPacketID = request.PacketID;
            p.ReplyCode = replyCode;
            p.ReplyPacketType = request.PacketTypeID;
            p.ReplyPacketSubType = request.PacketSubTypeID;
            p.ReplyMessage = msg;
            p.IsUDP = request.IsUDP;

            return p;
        }

        /// <summary>
        /// A single packet was received, with one or more network byte transfers
        /// </summary>
        protected virtual void OnPacketReceived()
        {
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

        protected virtual void OnFileStreamProgress(string path, long currentBytesDownloaded, long totalBytesToDownload)
        {
        }

        protected long m_CurrentDLTotalLength = -1;
        protected long m_CurrentDLCurrentDown = -1;
        protected string m_CurrentDLFilename = "";
        protected string m_CurrentDLDiskname = "";
        protected Stream m_CurrentFileStream = null;

        protected virtual void ProcessStreamPacket(INetworkConnection con, Packet packet)
        {            
        }

        /// <summary>
        /// This is the first method that gets called after it's deserialized.  This method ends up calling OnPacketReceived, depending on PacketTypeID,
        /// which then invokes the Registered handlers.  Sometimes, it may be of benefit to forgo the processing of the registered handlers (or at least delay them) until you've
        /// had a chance to examine the packet. 
        /// This is the method to hook into if you want to handle a type of packet in a specialized way.  Don't forget to call the base method if you don't handle the packet.  If you ha        
        /// a reason to override this method, you will need to call OnAfterPacketProcessed after you handle your msg.  This way you hook fully and 
        /// completely into the message processing loop.  Doing any of this, is considered fairly advanced functionality.  Don't attempt if you're not certain about what you're doing.
        /// </summary>
        /// <param name="p"></param>
        protected virtual void HandlePacket(Packet p)
        {
            //Log.LogMsg("==>Got packet " + newPacket.GetType().ToString());
            // Bubble the packet up to be processed
            OnPacketReceived(p);
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
                    Log.LogMsg("Exception thrown whilst processing packet type " + msg.PacketTypeID.ToString() + ", sub-type " + msg.PacketSubTypeID + ". Object = " + this.GetType().ToString() + ", Message: " + e.Message + ". Stack:\r\n " + e.StackTrace);
                }

                OnAfterPacketProcessed(msg);
                return;
            }

            KillConnection(this.GetType().ToString() + " did not have a registered packet handler for packet " + msg.PacketTypeID.ToString() + ", sub-type " + msg.PacketSubTypeID);
            //throw new NotImplementedException(this.GetType().ToString() + " did not implement packet handler for packet " + msg.GetType().ToString() + ".");
        }

        /// <summary>
        /// Builds a packet object, given a raw binary buffer and some misc info
        /// </summary>
        /// <param name="state"></param>
        public void DeserializePacket(SockState state)
        {
            try
            {
                OnPacketReceived();
                // at this point, the envelope bytes have been chopped off
                byte[] body = OnPacketDataArrived(state.PacketBuffer, 0, state.PacketBuffer.Length, state.Flags);
                if (body == null || body.Length < 1)
                {
                    KillConnection("Authentication/Encryption Key error.");
                    return;
                }

                //Log.LogMsg("Deserializing packet type " + state.PacketTypeID.ToString());
                Packet newPacket = CreatePacket(state.PacketTypeID, state.PacketSubTypeID, state.Flags, body);
                newPacket.PacketSubTypeID = state.PacketSubTypeID;
                newPacket.RemoteEndPoint = state.AsyncEventArgs.RemoteEndPoint as IPEndPoint;

                // Send delivery confirmation, check for duplicate packets, etc
                if (OnBeforePacketProcessed(newPacket))
                {
                    HandlePacket(newPacket);
                }
            }
            catch (Exception e)
            {
                Log.LogMsg("Exception in DeserializePacket: " + e.Message);
                KillConnection("Deserialize error. " + e.Message);
            }
        }

        public void Dispose()
        {
            Log.LogMsg("Disposing network connection.");
        }

        /// <summary>
        /// Shuts down the socket and cleans up resources. Also calls OnSocketKilled and fires SocketKilled event. Once a connection has been killed
        /// by whatever means, it can't be reused.
        /// </summary>
        public void KillConnection(string msg)
        {
            KillConnection(msg, true);
        }

        /// <summary>
        /// Override to catch socket killed occurance.  Be sure to call this base method, unless you do not want the SocketKilled event to fire
        /// </summary>
        /// <param name="msg"></param>
        protected virtual void OnSocketKilled(string msg)
        {
            FireSocketKilled(this, msg);
        }

        /// <summary>
        /// Shuts down the socket and cleans up resources. Also calls OnSocketKilled and fires SocketKilled event. Once a connection has been killed
        /// by whatever means, it can't be reused.
        /// </summary>
        public void KillConnection(string msg, bool allowPendingPacketsToSend)
        {
            if (m_ShuttingDown != 0)
            {
                return;
            }

            m_ShuttingDown = 1;

            try
            {
                OnSocketKilled(msg);
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
                    MyUDPSocket.Close();
                }

                MyTCPSocket = null;
                MyUDPSocket = null;

                //ShuttingDown = false;
                Log.LogMsg("KillConnection -->>----> " + msg);

                Dispose();
            }
           
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
                if (p.ReplyPacket != null && p.ReplyPacket.ReplyCode == ReplyType.AuthorizationTicketExpired) // NoAuth reply means the packet wasn't processed.  Remove it from the processed list.
                {
                    // it was not authorized, hence not processed.  remove it from the recently processed list
                    m_LastFewPacketsAnswered.Remove(p.PacketID);
                }
            }

            if (p.ReplyPacket != null)
            {
                if (p.NeedsReply)
                {
                   Send(p.ReplyPacket);
                }

                if (p.ReplyPacket.IsCritical && p.ReplyPacket.ReplyCode != ReplyType.OK)
                {
                    KillConnection("Critical packet failed. " + p.ReplyPacket.ReplyMessage);
                }
            }
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
        /// Called by  TransitStrategy when it has sent a complete packet
        /// </summary>
        public void PacketSent()
        {
            OnPacketSent();
        }

        /// <summary>
        /// A single packet was transferred, with one or more network byte transfers
        /// </summary>
        protected virtual void OnPacketSent()
        {

        }

        public bool ProcessIncomingPacketsImmediately
        {
            get
            {
                return true;
            }
            set
            {
                if (!value)
                {
                    Log.LogMsg("Warning: ProcessIncomingPacketImmediately is always true for UnityClientConnection. Value can't be changed.");
                }
            }
        }

        /// <summary>
        /// Sends out outgoing network packets that have previously been queued with Send()
        /// </summary>
        public void ProcessSend()
        {
            try
            {
                NetQItem item;
                while (m_SendQueue.Count > 0)
                {
                    item = m_SendQueue.Dequeue();
                    SendBytes(item.IsUDP, item.Data);
                }
            }
            catch (Exception e)
            {
                Log.LogMsg("DuplexBlockingTransit Failed to ProcessSend. " + e.Message);
            }
        }

        private void SendBytes(bool isUDP, byte[] data)
        {
            try
            {
                if (!IsConnected)
                {
                    Log.LogMsg("UnityClientConnection can't send bytes. Not connected.");
                    return;
                }

                if (isUDP)
                {
                    MyUDPSocket.SendTo(data, UDPSendTarget);
                }
                else
                {
                    MyTCPSocket.Blocking = true;
                    MyTCPSocket.Send(data);
                }

                SentBytes(data);
                PacketSent();
            }
            catch (Exception e)
            {
                Log.LogMsg("Error SendBytes. " + e.Message);
                KillConnection("Send error. " + e.Message);
            }
        }

        /// <summary>
        /// Blocking receive TCP call.
        /// </summary>
        /// <returns></returns>
        public bool ProcessReceive()
        {
            try
            {
                MyTCPSocket.Blocking = true;
                if (IsAlive && MyTCPSocket.Poll(0, SelectMode.SelectRead))
                {
                    m_TCPBytesReceived = MyTCPSocket.Receive(m_TCPBuffer, 0, m_TCPBuffer.Length, SocketFlags.None, out m_LastTCPReceiveStatus);
                    OnReceiveResolved(false, m_TCPBuffer, m_TCPBytesReceived, m_LastTCPReceiveStatus, m_TCPSockState);
                }
                else if (!IsAlive)
                {
                    Log.LogMsg("UnityClientConnection can't ProcessReceive because it's not connected.");
                    KillConnection("Couldn't receive. Connection has been closed.");
                }
            }
            catch (Exception e)
            {
                string error = "";// OwningConnection.MyTCPSocket.GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.Error).ToString();
                Log.LogMsg("Error receiving data on DuplexBlockingTransit. (" + error + ")" + e.Message + ", Stack=\r\n" + e.StackTrace);
                KillConnection("Connection lost! Network receive error: " + e.Message);
            }
            return true;
        }

        /// <summary>
        /// Gets called when a receive operation resolves.  If we were listening for data and the connection
        /// closed, that also counts as a receive operation resolving.
        /// </summary>
        /// <param name="args"></param>
        private void OnReceiveResolved(bool isUDP, byte[] buffer, int bytesReceived, SocketError status, SockState sockState)
        {
            try
            {
                if (!IsAlive && bytesReceived > 0)
                {
                    return;
                }

                // If there was a socket error, close the connection. This is NOT a normal
                // situation, if you get an error here.
                if (status != SocketError.Success)
                {
                    if (!ShuttingDown)
                    {
                        KillConnection("Connection lost! Network receive error: " + status);
                    }
                    //Jump out of the ProcessReceive method.
                    return;
                }

                m_TCPSockState.AsyncEventArgs.RemoteEndPoint = MyTCPSocket.RemoteEndPoint;

                // If no data was received, close the connection. This is a NORMAL
                // situation that shows when the client has finished sending data.
                if (bytesReceived == 0)
                {
                    if (!ShuttingDown)
                    {
                        KillConnection("Connection closed by remote host.");
                    }
                    return;
                }

                ReceivedBytes(bytesReceived);

                // restart listening process    
                AssembleInboundPacket(buffer, bytesReceived, sockState);
            }
            catch (Exception ex)
            {
                Log.LogMsg("Error ProcessReceive. " + ex.Message);
                KillConnection("Error receive. " + ex.Message);
            }
        }

        /// <summary>
        /// If ProcessPacketsImmediately == false, you must call this method to process all queued networking packets.
        /// No packets are ACKnowledged or replied to until this method is called. Calling this method clears out the inbox.
        /// </summary>
        /// <returns></returns>
        public virtual int ProcessNetworking()
        {
            if(!IsAlive)
            {
                return -1;
            }

            try
            {
                ProcessSend();   
                ProcessReceive();                             
            }
            catch (Exception e)
            {
                Log.LogMsg("Exception thrown while processing networking. " + e.Message + "\r\n" + e.StackTrace);
                KillConnection("Unable to process networking. Connection aborted.");
            }

            return 1;
        }

        /// <summary>
        /// Called by TransitStrategy objects when it received a byte. Ends up firing ByteReceived events.
        /// </summary>
        /// <param name="count"></param>
        public void ReceivedBytes(int count)
        {
            OnBytesReceived(count);
        }

        /// <summary>
        /// A byte receive was completed, consisting either of one packet or a fragment thereof.
        /// </summary>
        /// <param name="data"></param>
        protected virtual void OnBytesReceived(int count)
        {
        }

        /// <summary>
        /// Serializes and sends a packet across the wire
        /// </summary>
        /// <param name="msg">The packet to send</param>
        public virtual int Send(Packet msg)
        {
            try
            {
                if (msg.NeedsDeliveryAck)
                {
                    m_NumAcksWaitingFor++;
                }

                byte[] raw = SerializePacket(msg);
                return Send(raw, msg.Flags);
            }
            catch (Exception sendExc)
            {
                KillConnection(sendExc.Message);
                return -1; ;
            }
        }

        /// <summary>
        /// Sends arbitrary bytes of data across the wire.  Note that if the remote endpoint can't decipher the data
        /// as a known packet, the connection will be dropped by that endpoint immediately.  
        /// </summary>
        /// <param name="data">bytes to send</param>
        public virtual int Send(byte[] data, PacketFlags flags)
        {
            //Log.LogMsg("@__@ Monitor Enter");
            if (!IsAlive)
            {
                KillConnection("Couldn't send. Connection has been closed.");
                return -1;
            }
            try
            {
                bool isUDP = (flags & PacketFlags.UDP) != 0;
                if (isUDP)
                {
                    if (!CanSendUDP)
                    {
                        Log.LogMsg("!!! Tried sending UDP packet when the connection wasn't yet ready to send UDP. Dropping packet.");
                        return 0;
                    }

                    // Don't allow fragmenting UDP packets
                    if (data.Length > 1024)
                    {
                        Log.LogMsg("Message exceeded UDP size of 1024 bytes. Sending via TCP instead.");
                        flags &= ~PacketFlags.UDP;
                    }
                }

                NetQItem qi = new NetQItem();
                qi.Flags = flags;
                qi.Data = data;
                qi.IsUDP = isUDP;
                m_SendQueue.Enqueue(qi);
                return 1;
            }
            catch (Exception sendExc)
            {
                Log.LogMsg("DuplexBlockingTransit - Failed to send data. " + sendExc.Message);
                return -1; ;
            }

            return 1;
        }

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

                    PacketStream p = CreatePacket((int)PacketType.PacketStream, subType, false, false) as PacketStream;
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

                    Send(p);
                }

                DateTime now = DateTime.Now;
                TimeSpan duration = now - start;
                string msg = string.Format("Sent {0} MB file {1} to {2} in {3}.", new object[] { Util.ConvertBytesToMegabytes(fi.Length).ToString("N"), fi.Name, RemoteEndPoint.ToString(),/* duration.ToString("hh\\:mm\\:ss\\:ff")*/ duration.TotalSeconds.ToString() + " Minutes" });
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
            return Send(p) == 1;
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

        public bool SendPacketAck(Packet packet)
        {
            bool rslt = true;

            PacketACK p = (PacketACK)CreatePacket((int)PacketType.ACK, 0, false, false);
            p.ReplyPacketID = packet.PacketID;
            p.ReplyCode = ReplyType.OK;
            p.ReplyPacketType = packet.PacketTypeID;
            p.ReplyPacketSubType = packet.PacketSubTypeID;
            p.IsUDP = packet.IsUDP;

            Send(p);
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

            Send(p);
            return rslt;
        }

        /// <summary>
        /// A byte transfer was completed, consisting either of one packet or a fragment thereof.
        /// </summary>
        /// <param name="data"></param>
        protected void OnBytesTransferred(byte[] data)
        {
            OnBytesSent(data.Length);
        }

        /// <summary>
        /// A byte transfer was completed, consisting either of one packet or a fragment thereof.
        /// </summary>
        /// <param name="data"></param>
        protected virtual void OnBytesSent(int count)
        {
        }
        
        /// <summary>
        /// Called by TransitStrategy objects when it has sent a number of bytes
        /// </summary>
        /// <param name="count"></param>
        public void SentBytes(byte[] bytes)
        {
            OnBytesTransferred(bytes);
        }

        /// <summary>
        /// Called by TransitStrategy objects when it has sent a number of bytes
        /// </summary>
        /// <param name="count"></param>
        public void SentBytes(int count)
        {
            OnBytesSent(count);
        }



        /// <summary>
        /// Serializes a network packet in preparation of sending it across the wire.  When broadcasting packets to multiple recipients,
        /// it can be helpful to manually serialize the packet via this method and then send out the serialized bytes to all targets
        /// as opposed to serializing the data for each transmission.  Note, that if a packet is encrypted this strategy will fail,
        /// as encrypted packets, being encrypted with the target's unique key, can only be deserialized by the target recipient.
        /// When broadcasting packets in this manner, it may also be prudent to replace the Packet.Header.ToEndpointID string with something
        /// like "BROADCAST" to prevent sending sensitive information like the normally embedded account name to people in the broadcast list
        /// </summary>
        /// <param name="msg">the packet to serialize</param>
        /// <returns>the byte array version of the packet</returns>
        public virtual byte[] SerializePacket(Packet msg)
        {
            TimestampOutgoingPacket(msg);
            // assign a new ID
            msg.PacketID = Packet.NextPacketId;

            // Body
            Pointer bodyDataPointer = new Pointer();

            byte[] body = msg.Serialize(bodyDataPointer);

            // Encrypt and compress, if applicable
            body = OnBeforeSendPacket(body, msg.IsEncrypted, msg.IsCompressed, bodyDataPointer);

            // Envelope
            byte[] envelope = new byte[13]; // Flags/byte + MessageLength/Int32 + PacketType/Int32 + PacketSubType/Int32

            // Total message length // Int32
            int msgLen = bodyDataPointer.Position;
            BitConverter.GetBytes(msgLen).CopyTo(envelope, 0);

            // Packet type // Int32
            BitConverter.GetBytes(msg.PacketTypeID).CopyTo(envelope, 4);

            // Packet sub type // byte
            BitConverter.GetBytes(msg.PacketSubTypeID).CopyTo(envelope, 8);// envelope[8] = msg.PacketSubTypeID;

            // Flags // byte
            envelope[12] = (byte)msg.Flags;

            // Combine envelope and body into final data gram
            byte[] dataGram = new byte[bodyDataPointer.Position + envelope.Length];

            Util.Copy(envelope, 0, dataGram, 0, envelope.Length);
            Util.Copy(body, 0, dataGram, envelope.Length, bodyDataPointer.Position);

            //Log.LogMsg("==>Serializing " + msg.GetType().ToString() + " : " + dat.Length.ToString());
            return dataGram;
        }

        /// <summary>
        /// Called by UDP listeners when they receive an UDP packet
        /// </summary>
        public void SetLastUDPACKReceived(DateTime when)
        {
            LastUDPACKReceived = when;
        }

        protected virtual void OnInitialize()
        {
			LastUDPACKReceived = DateTime.MinValue;
            m_TCPSockState = new SockState(null, 0, null);
            m_TCPSockState.AsyncEventArgs = new SocketAsyncEventArgs();
            if (!m_IsInitialized)
            {
                NetworkConnection.RegisterPacketCreationDelegate((int)PacketType.PacketGenericMessage, delegate { return new PacketGenericMessage(); });
                NetworkConnection.RegisterPacketCreationDelegate((int)PacketType.LineSecured, delegate { return new PacketLineSecured(); });
                NetworkConnection.RegisterPacketCreationDelegate((int)PacketType.PacketRijndaelExchangeRequest, delegate { return new PacketRijndaelExchangeRequest(); });
                NetworkConnection.RegisterPacketCreationDelegate((int)PacketType.PacketGameServerAccessGranted, delegate { return new PacketGameServerTransferResult(); });
                NetworkConnection.RegisterPacketCreationDelegate((int)PacketType.LoginRequest, delegate { return new PacketLoginRequest(); });
                NetworkConnection.RegisterPacketCreationDelegate((int)PacketType.LoginResult, delegate { return new PacketLoginResult(); });
                NetworkConnection.RegisterPacketCreationDelegate((int)PacketType.Null, delegate { return new PacketNull(); });
                NetworkConnection.RegisterPacketCreationDelegate((int)PacketType.RequestHandoffToServer, delegate { return new PacketRequestHandoffToServerCluster(); });
                NetworkConnection.RegisterPacketCreationDelegate((int)PacketType.RijndaelExchange, delegate { return new PacketRijndaelExchange(); });
                NetworkConnection.RegisterPacketCreationDelegate((int)PacketType.PacketStream, delegate { return new PacketStream(); });
                NetworkConnection.RegisterPacketCreationDelegate((int)PacketType.CharacterListing, delegate { return new PacketCharacterListing(); });
                NetworkConnection.RegisterPacketCreationDelegate((int)PacketType.GenericReply, delegate { return new PacketReply(); });
                NetworkConnection.RegisterPacketCreationDelegate((int)PacketType.ACK, delegate { return new PacketACK(); });
                NetworkConnection.RegisterPacketCreationDelegate((int)PacketType.NATInfo, delegate { return new PacketNATInfo(); });
                NetworkConnection.RegisterPacketCreationDelegate((int)PacketType.ClockSync, delegate { return new PacketClockSync(); });


                m_IsInitialized = true;
            }

            RegisterPacketHandler((int)PacketType.ACK, OnPacketACK);
            RegisterPacketHandler((int)PacketType.ClockSync, OnClockSync);
            RegisterPacketHandler((int)PacketType.PacketStream, ProcessStreamPacket);
            RegisterPacketHandler((int)PacketType.GenericReply, OnPacketGenericReply);

            Roles = new string[0];
            RegisterPacketHandler((int)PacketType.PacketRijndaelExchangeRequest, OnRijndaelExchangeRequest);
            RegisterPacketHandler((int)PacketType.LineSecured, OnLineSecured);
            RegisterPacketHandler((int)PacketType.LoginResult, OnLoginResult);
            RegisterPacketHandler((int)PacketType.PacketGameServerAccessGranted, OnGameServerAccessInfoArrived);

        }

        #region SocketConnectionConcluded Event
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

        private void FireSocketConnectionConcluded(UnityClientConnection con, bool result, string msg)
        {
            if (SocketConnectionConcludedInvoker != null)
            {
                SocketConnectionConcludedInvoker(con, result, msg);
            }
        }
        #endregion

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

        private void FireConnectedEvent(bool success, string p_2)
        {
            m_ConnectionInProgress = false;
            if (!success)
            {
                KillConnection(p_2);
            }
            OnConnected(success, p_2);
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
        /// Begin an synchronous connection attempt
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

            ResolveHostNameAsync();
        }

        private void ResolveHostNameAsync()
        {
            Log.LogMsg("Resolving host async [" + m_ServerAddress + "]");
            try
            {
                Dns.BeginGetHostAddresses(m_ServerAddress, OnHostResolveComplete, null);
            }
            catch (Exception e)
            {
                FireConnectedEvent(false, "Unable to resolve server IP Address.");
            }
        }

        private void OnHostResolveComplete(IAsyncResult result)
        {
            try
            {
                Log.LogMsg("Resolving host async complete.");
                IPAddress[] host = Dns.EndGetHostAddresses(result);
                //IPAddress[] host = (IPAddress[])result.AsyncState;
                OnHostResolve(host);
            }
            catch (Exception e)
            {
                FireConnectedEvent(false, "Unable to resolve server IP Address.");
            }
        }

        private void ResolveHostNameSync()
        {
            Log.LogMsg("Resolving host sync [" + m_ServerAddress + "]");
            try
            {
                IPAddress[] host = Dns.GetHostAddresses(m_ServerAddress);
                OnHostResolve(host);
            }
            catch (Exception e)
            {
                FireConnectedEvent(false, "Unable to resolve server IP Address.");
            }
        }

        /// <summary>
        /// Fires when the target address has been resolved.
        /// </summary>
        /// <param name="success">true, if the address could be resolved</param>
        /// <param name="resolvedAddress">the IP address, or null if not resolved</param>
        protected virtual void OnHostnameResolved(bool success, IPAddress resolvedAddress)
        {
        }

        private void OnHostResolve(IPAddress[] host)
        {            
            try
            {
                // Check to make sure we got at least one IP address, otherwise we can stop right now.
                bool gotOne = false;
                IPAddress ip = null;
                bool useIP6 = false; // ConfigHelper.GetStringConfig("UseIPv6").ToLower() == "true";
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
                MyTCPSocket = new Socket(useIP6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                //MyTCPSocket.Bind(new IPEndPoint(useIP6 ? IPAddress.IPv6Any : IPAddress.Any, 0));

                //MyUDPSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                //MyUDPSocket.ExclusiveAddressUse = false;

                MyTCPSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                MyTCPSocket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
                //MyTCPSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.OutOfBandInline, true);

                Log.LogMsg("Connecting to host [" + ip + "] ...");
                
                /***********************                
                MyTCPSocket.Connect(ipep);
                OnConnectionAttemptConcluded();
                **************************/

                /***********************
                SocketAsyncEventArgs arg = new SocketAsyncEventArgs();
                arg.Completed += new EventHandler<SocketAsyncEventArgs>(OnAsyncConnectCompleted);
                arg.RemoteEndPoint = ipep;
                arg.UserToken = MyTCPSocket;
                MyTCPSocket.ConnectAsync(arg);
                ************************/

                
                MyTCPSocket.BeginConnect(ipep, new AsyncCallback(BeginConnectEnd), null);
                /******************/
            }
            catch (Exception e)
            {
                FireConnectedEvent(false, "Unable to connect to server. " + e.Message);
                return;
            }
        }

        private AutoResetEvent m_AutoResetEvent = new AutoResetEvent(false);

        private void BeginConnectEnd(IAsyncResult asyncresult)
        {
            try
            {
                MyTCPSocket.EndConnect(asyncresult);
                OnConnectionAttemptConcluded();
            }
            catch (Exception ex)
            {
                FireConnectedEvent(false, "Unable to connect to server. " + ex.Message);
            }
        }

        private void OnAsyncConnectCompleted(object sender, SocketAsyncEventArgs args)
        {
            args.Completed += new EventHandler<SocketAsyncEventArgs>(OnAsyncConnectCompleted);
            if (args.SocketError == SocketError.Success)
            {
                OnConnectionAttemptConcluded();
            }
            else
            {
                FireConnectedEvent(false, "Unable to connect to server. " + args.SocketError.ToString());
            }
        }

        private void OnConnectionAttemptConcluded()
        {
            m_ConnectionInProgress = false;
            FireConnectedEvent(true, "Endpoint " + RemoteEndPoint.ToString());

            try
            {
                // send serviceId request 
                Log.LogMsg("Sending service ID " + ServiceID.ToString());
                Send(BitConverter.GetBytes(ServiceID), PacketFlags.IsCritical);
            }
            catch (Exception con2Exc)
            {
                KillConnection(con2Exc.Message);
            }
        }

        private void OnGameServerAccessInfoArrived(INetworkConnection con, Packet p)
        {
            PacketGameServerTransferResult packetGameServerAccessGranted = p as PacketGameServerTransferResult;

            Log.LogMsg("Server Transfer to [" + packetGameServerAccessGranted.ReplyCode.ToString() + " / " + packetGameServerAccessGranted.ReplyMessage + " / " + packetGameServerAccessGranted.ServerName + " / " + packetGameServerAccessGranted.ServerIP + "] for resource [" + packetGameServerAccessGranted.TargetResource.ToString() + "].");

            FireServerTransferDirective(this, packetGameServerAccessGranted);

            KillConnection(packetGameServerAccessGranted.ReplyCode == ReplyType.OK ? "Transferred." : "Not Transferred.");
            OnServerTransferDirective(packetGameServerAccessGranted);
        }
       
        protected virtual void OnServerTransferDirective(PacketGameServerTransferResult msg)
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

        protected virtual void OnPacketAuthFailed(PacketReply replyPacket)
        {
        }

        /// <summary>
        /// Gets called when we receive a clock synchronization packet
        /// </summary>
        /// <param name="sender">the connection sending the sync packet</param>
        /// <param name="msg">the sync data</param>
        protected virtual void OnClockSync(INetworkConnection sender, Packet msg)
        {
            OnClockSync(msg as PacketClockSync);
        }

        /// <summary>
        /// Send out packet failure messages to listeners, if application
        /// </summary>
        private void OnPacketACK(INetworkConnection con, Packet msg)
        {
            OnPacketReceiptACK(msg as PacketACK);
        }

        protected virtual void OnPacketReceiptACK(PacketACK msg)
        {
            m_NumAcksWaitingFor--;
            if (msg.IsUDP)
            {
                LastUDPACKReceived = DateTime.UtcNow;
                Log.LogMsg("Got UDP ACK from " + RemoteEndPoint.ToString());
            }
        }

        #region PacketHandlers

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
        /// Removes a previously registered delegate from the packet handler map
        /// </summary>
        /// <param name="packetType">the packet type to remove the handler for</param>
        /// <param name="handlerMethod">the method to unregister</param>
        public void UnregisterPacketHandler(int packetType, int packetSubType, Action<INetworkConnection, Packet> handlerMethod)
        {
            m_PacketHandlerMap.UnregisterHandler(packetType, packetSubType, handlerMethod);
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

        #endregion

        #endregion

        double m_sincelastread = 0;
        /// <summary>
        /// Call this method from the game update loop and pass in Time.deltaTime.
        /// It is used for timers inside the network connection.  It prevents us from
        /// having to use System.Timers.  Best to keep multithreading out of the game loop.
        /// </summary>
        /// <param name="deltaSecs"></param>
        public virtual void Update(double deltaSecs)
        {
            m_sincelastread += deltaSecs;
            if(NatPokeInterval > -1 && m_sincelastread > 10)
            {
                m_sincelastread = 0;
                OnUDPKeepAlive();
            }
        }

        void OnUDPKeepAlive()
        {
			//Log.LogMsg("Sending UDP Keep Alive ...");
            if (m_UDPListener != null && m_UDPListener.Port > 0)
            {
                SendUDPPoke(m_UDPListener.Port);
            }
        }

        /// <summary>
        /// Sends a UDP null packet, to which the recipient will, at the lesst, reply to with an ACK
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

        public DateTime LastUDPPokeSent = DateTime.MinValue;

        #region ClientData

        /// <summary>
        ///Used to synchronize network time between this connection and another.
        /// </summary>
        public NetworkClock Clock { get; private set; }

        /// <summary>
        /// Listens to all UDP traffic for our listen-on port.
        /// </summary>
        protected IUDPListener m_UDPListener;

        protected double NatPokeInterval = 15;
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
                    double natPokeInterval = ConfigHelper.GetIntConfig("NatPokeInterval", 10); // seconds
                    if (natPokeInterval < 10) // no faster than every 10 seconds
                    {
                        natPokeInterval = 10;
                    }
                    NatPokeInterval = natPokeInterval; // don't start it yet.  not until the connection is established.
                }
                else
                {
                    NatPokeInterval = -1;
                }
            }
        }

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

        #region BeforeLoginRequest Event
        private BeforeLoginRequestDelegate BeforeLoginRequestInvoker;
        public delegate void BeforeLoginRequestDelegate(PacketLoginRequest req);

        /// <summary>
        /// Fires when the login server responds to our login request.
        /// </summary>
        public event BeforeLoginRequestDelegate BeforeLoginRequest
        {
            add
            {
                AddHandler_BeforeLoginRequest(value);
            }
            remove
            {
                RemoveHandler_BeforeLoginRequest(value);
            }
        }
        private void AddHandler_BeforeLoginRequest(BeforeLoginRequestDelegate value)
        {
            BeforeLoginRequestInvoker = (BeforeLoginRequestDelegate)Delegate.Combine(ServerLoginResultArrivedInvoker, value);
        }
        private void RemoveHandler_BeforeLoginRequest(BeforeLoginRequestDelegate value)
        {
            BeforeLoginRequestInvoker = (BeforeLoginRequestDelegate)Delegate.Remove(ServerLoginResultArrivedInvoker, value);
        }
        protected void FireBeforeLoginRequest(PacketLoginRequest req)
        {
            if (BeforeLoginRequestInvoker != null)
            {
                BeforeLoginRequestInvoker(req);
            }
        }
        #endregion
 
        protected virtual byte[] OnBeforeSendPacket(byte[] body, bool encrypt, bool compress, Pointer bodyPointer)
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

        protected virtual void OnClockSync(PacketClockSync msg)
        {
            Clock.AddSample(msg.StartTime, DateTime.UtcNow.Ticks, msg.SentOnUTC);
        }

        protected virtual void TimestampOutgoingPacket(Packet msg)
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
            FireBeforeLoginRequest(req);
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
