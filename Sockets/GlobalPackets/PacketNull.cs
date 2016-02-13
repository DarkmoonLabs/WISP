using System;

namespace Shared
{
	/// <summary>
	/// Empty packet, practically used for Pings
	/// </summary>
	public class PacketNull : Packet
	{
		public PacketNull(){}

		public PacketNull(ulong endpoint, bool encryptHeader, bool encryptBody, bool compress)
		{
            
		}

        public override byte[] Serialize(Pointer p)
        {
            base.Serialize(p);
            BitPacker.AddBool(ref m_SerializeBuffer, p, true);
            return m_SerializeBuffer;
        }

        public override bool DeSerialize(byte[] dat, Pointer p)
        {
            base.DeSerialize(dat, p);
            BitPacker.GetBool(dat, p);
            return true;
        }
	}
}
