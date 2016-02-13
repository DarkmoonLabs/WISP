using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Shared;
using System.Net.Sockets;
using System.Threading;
using System.Net;

namespace Shared
{
    public abstract partial class NetworkConnection
    {
        private ITransitStrategy m_Transit;
        /// <summary>
        /// The algorithm that handles receiving/sending data
        /// </summary>
        protected ITransitStrategy Transit
        {
            get { return m_Transit; }
            set { m_Transit = value; }
        }

        /// <summary>
        /// Assembles the packet across however many frames it takes
        /// </summary>
        /// <param name="data"></param>
        public void AssembleInboundPacket(SocketAsyncEventArgs args, SockState state)
        {
            AssembleInboundPacket(args.Buffer, args.BytesTransferred, state);
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
                    // do not ever delay clock sync packets, no matter what. clock sync packets are used to 
                    // calculate latencies and to sync game clocks. waiting to respond is not an option, but since
                    // the handler for this packet is deep in the root NetworkConnection hiearchy, it shouldn't
                    // intefere, even with a client that otherwise delays packet handling
                    if (newPacket.PacketTypeID == (int)PacketType.ClockSync)
                    {
                        HandlePacket(newPacket);
                    }

                    // Determine if the packet should be handled immediately or late
                    else if (ProcessIncomingPacketsImmediately)
                    {
                        HandlePacket(newPacket);
                    }
                    else
                    {
                        lock (m_InboxLocker)
                        {
                            m_Inbox.Enqueue(newPacket);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.LogMsg("Exception in DeserializePacket: " + e.Message);
                KillConnection("Deserialize error. " + e.Message);
            }
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

        protected virtual void TimestampOutgoingPacket(Packet msg)
        {
            msg.SentOnUTC = DateTime.UtcNow.Ticks;
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
    /// A byte transfer was completed, consisting either of one packet or a fragment thereof.
    /// </summary>
    /// <param name="data"></param>
    protected void OnBytesTransferred(byte[] data)
    {
        OnBytesSent(data.Length);
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
    /// A byte transfer was completed, consisting either of one packet or a fragment thereof.
    /// </summary>
    /// <param name="data"></param>
    protected virtual void OnBytesSent(int count)
    {
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

        /// <summary>
        /// A single packet was received, with one or more network byte transfers
        /// </summary>
        protected virtual void OnPacketReceived()
        {
        }

       


    }
}
