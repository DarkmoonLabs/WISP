using System;

namespace Shared
{
	/// <summary>
	/// Contains the encrypted rijndael exchange
    /// <para>
    /// Results in a PacketLineSecured reply
    /// </para>
	/// </summary>
    public class PacketRijndaelExchange : PacketReply
	{
		public PacketRijndaelExchange()
		{
            IsCritical = true;
		}

		private byte[] m_rijndaelExchange = new byte[0];
		public byte[] RijndaelExchangeData
		{
			get
			{
				return m_rijndaelExchange;
			}
			set
			{
				m_rijndaelExchange = value;
			}
		}

        private byte[] m_PublicRSAKey = new byte[0];
        public byte[] PublicRSAKey
        {
            get { return m_PublicRSAKey; }
            set { m_PublicRSAKey = value; }
        }

        public override byte[] Serialize(Pointer p)
		{
            base.Serialize(p);
            BitPacker.AddBytes(ref m_SerializeBuffer, p, RijndaelExchangeData);
            BitPacker.AddBytes(ref m_SerializeBuffer, p, PublicRSAKey);
            return m_SerializeBuffer;
		}

        public override bool DeSerialize(byte[] data, Pointer p)
		{
            base.DeSerialize(data, p);
            m_rijndaelExchange = BitPacker.GetBytes(data, p);
            PublicRSAKey = BitPacker.GetBytes(data, p);
			return true;
		}


	}
}
