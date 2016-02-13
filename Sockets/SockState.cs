using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Text; //for testing

namespace Shared
{
    /// <summary>
    /// Internally used class to capture TCP socket state during network communication.
    /// </summary>
    public class SockState
    {       
        private int m_ID;
        public int ID 
        {
            get
            {
                return m_ID;
            }
            set
            {
                m_ID = value;
            }
        }
        /// <summary>
        /// The length, in bytes, of the networking packet preamble that tells us how many bytes follow, given our protocol
        /// </summary>
        public static int EnvelopeLength = 13;

        /// <summary>
        /// the connection from which this sockstate was last used
        /// </summary>
        public INetworkConnection Operator;

        public byte[] PacketBuffer = new byte[EnvelopeLength]; // should be initialized when we know how big the packet is
        /// <summary>
        /// the offset in the BufferManager to our chunk of data
        /// </summary>
        public int BufferBlockOffset = 0; 
        
        /// <summary>
        /// For Read: How much we've currently read into the PacketBuffer (the packet buffer is allocated for the whole message, which may not arrive immediately)
        /// <para>
        /// For Write: How much of the PacketBuffer we've current written (the packet buffer is sent in chunks, the whole message may not be sent at once)
        /// </para>
        /// </summary>
        public Pointer PacketBufferPointer = new Pointer();
        public int BufferBlockLength;
        public long MessageLength = -1; // Int64
        public PacketFlags Flags = PacketFlags.None;
        public int PacketTypeID = 0;
        public int PacketSubTypeID = 0;
        public SocketAsyncEventArgs AsyncEventArgs { get; set; }
        public SocketAsyncEventArgsStack Pool;
        
        /// <summary>
        /// Cached objects go back into the SocketAsyncEventArgsCache when they get pushed.
        /// </summary>
        public bool IsCached = true;
        public SockState(SocketAsyncEventArgs eventArgs, int bufferBlockLength, SocketAsyncEventArgsStack owner)
        {
            Pool = owner;
            AsyncEventArgs = eventArgs;
            if (eventArgs != null)
            {
                BufferBlockOffset = eventArgs.Offset;
            }
            BufferBlockLength = bufferBlockLength;
        }

        public void Reset()
        {
            PacketBuffer = new byte[EnvelopeLength];
            PacketBufferPointer.Reset();
            MessageLength = -1;
        }

    }
}
