using System;
using System.Collections.Generic;
using System.Text;

namespace Shared
{
    /// <summary>
    /// Packet used to send a continuous stream of data
    /// </summary>
    public class PacketStream : PacketReply
    {
        public PacketStream() : this(Packet.SERIALIZE_BUFFER_SIZE)
        {            
        }

        public PacketStream(int bufferSize)
        {
            PacketTypeID = (int)PacketType.PacketStream;
            m_SerializeBuffer = new byte[bufferSize + 500];
        }

        private byte[] m_Buffer = new byte[0];
        /// <summary>
        /// The data being sent
        /// </summary>
        public byte[] Buffer
        {
            get { return m_Buffer; }
            set { m_Buffer = value; }
        }
        

        private string m_Description = "";
        public string Description
        {
            get { return m_Description; }
            set { m_Description = value; }
        }

        private long m_TotalLength = -1;
        public long TotalLength
        {
            get { return m_TotalLength; }
            set { m_TotalLength = value; }
        }

        private bool m_Final = false;
        /// <summary>
        /// Is this the final packet
        /// </summary>
        public bool Final
        {
            get { return m_Final; }
            set { m_Final = value; }
        }

        private bool m_Initial = false;
        /// <summary>
        /// Is this the initial packet
        /// </summary>
        public bool Initial
        {
            get { return m_Initial; }
            set { m_Initial = value; }
        }

        private string m_Arg = "";

        public string Arg
        {
            get { return m_Arg; }
            set { m_Arg = value; }
        }

        public override bool DeSerialize(byte[] data, Pointer p)
        {
            base.DeSerialize(data, p);
            Description = BitPacker.GetString(data, p);
            TotalLength = BitPacker.GetLong(data, p);
            Final = BitPacker.GetBool(data, p);
            Initial = BitPacker.GetBool(data, p);
            Arg = BitPacker.GetString(data, p);
            Buffer = BitPacker.GetBytes(data, p);
            return true;
        }

        public override byte[] Serialize(Pointer p)
        {
            base.Serialize(p);
            BitPacker.AddString(ref m_SerializeBuffer, p, Description);
            BitPacker.AddLong(ref m_SerializeBuffer, p, TotalLength);
            BitPacker.AddBool(ref m_SerializeBuffer, p, Final);
            BitPacker.AddBool(ref m_SerializeBuffer, p, Initial);
            BitPacker.AddString(ref m_SerializeBuffer, p, Arg);
            BitPacker.AddBytes(ref m_SerializeBuffer, p, Buffer);
            return m_SerializeBuffer;
        }
    }
}
