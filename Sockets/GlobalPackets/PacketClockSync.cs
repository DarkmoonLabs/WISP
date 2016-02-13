using System;

namespace Shared
{
    /// <summary>
    /// Packet sent when the RSA/Rijndael exchange successfully happened.  Contains the Rijndael key.
    /// </summary>
    public class PacketClockSync : PacketReply
    {
        public PacketClockSync()
        {
            IsCritical = false;
            PacketTypeID = (int)PacketType.ClockSync;
        }

        /// <summary>
        /// Either the current time or the 
        /// </summary>
        public long StartTime { get; set; }

        public override bool DeSerialize(byte[] data, Pointer p)
        {
            base.DeSerialize(data, p);
            StartTime = BitPacker.GetLong(data, p);
            return true;
        }

        public override byte[] Serialize(Pointer p)
        {
            base.Serialize(p);
            BitPacker.AddLong(ref m_SerializeBuffer, p, StartTime);
            return m_SerializeBuffer;
        }
    }
}
