using System;

namespace Shared
{
    /// <summary>
    /// Packet sent when the RSA/Rijndael exchange successfully happened.  Contains the Rijndael key.
    /// </summary>
    public class PacketLineSecured : PacketReply
    {
        public PacketLineSecured()
        {
            IsCritical = true;
        }

        private byte[] m_Key;

        public byte[] Key
        {
            get { return m_Key; }
            set { m_Key = value; }
        }


        public override bool DeSerialize(byte[] data, Pointer p)
        {
            base.DeSerialize(data, p);
            Key = BitPacker.GetBytes(data, p);
            return true;
        }

        public override byte[] Serialize(Pointer p)
        {
            base.Serialize(p);
            BitPacker.AddBytes(ref m_SerializeBuffer, p, Key);
            return m_SerializeBuffer;
        }
    }
}
