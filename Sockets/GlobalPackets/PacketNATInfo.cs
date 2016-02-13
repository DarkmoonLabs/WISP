using System;

namespace Shared
{
    /// <summary>
    /// Empty packet, practically used for Pings
    /// </summary>
    public class PacketNATInfo : Packet
    {
        public PacketNATInfo() { }

        public PacketNATInfo(ulong endpoint, bool encryptHeader, bool encryptBody, bool compress)
        {

        }

        /// <summary>
        /// the port on which we are listening for UDP traffic
        /// </summary>
        public int ListenOnPort { get; set; }


        public override byte[] Serialize(Pointer p)
        {
            base.Serialize(p);
            BitPacker.AddInt(ref m_SerializeBuffer, p, ListenOnPort);
            return m_SerializeBuffer;
        }

        public override bool DeSerialize(byte[] dat, Pointer p)
        {
            base.DeSerialize(dat, p);
            ListenOnPort = BitPacker.GetInt(dat, p);
            return true;
        }
    }
}
