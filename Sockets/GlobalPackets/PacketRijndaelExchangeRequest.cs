using System;

namespace Shared
{
    /// <summary>
    /// First packet sent by the server after a client connects. Contains server version info.
    /// <para>
    /// Results in a PacketRijndaelExchange reply.
    /// </para>
    /// </summary>
    public class PacketRijndaelExchangeRequest : Packet
    {
        public PacketRijndaelExchangeRequest()
        {
            ServerName = "Default";
            MaxPlayers = -1;
            IsCritical = true;
        }

        private Version m_ServerVersion = new Version(1, 0, 0, 0);
        public Version ServerVersion
        {
            get
            {
                return m_ServerVersion;
            }
            set
            {
                m_ServerVersion = value;
            }
        }

        private byte[] m_PublicRSAKey;
        public byte[] PublicRSAKey
        {
            get { return m_PublicRSAKey; }
            set { m_PublicRSAKey = value; }
        }

        private int m_ConnectionKeySize;
        public int ConnectionKeySize
        {
            get { return m_ConnectionKeySize; }
            set { m_ConnectionKeySize = value; }
        }

        public string ServerName { get; set; }
        public int MaxPlayers { get; set; }
        public int CurrentPlayers { get; set; }


        public override bool DeSerialize(byte[] data, Pointer p)
        {
            //Log.LogMsg("Attempting deserialize Rijndael Request.");
            base.DeSerialize(data, p);
            this.m_ServerVersion = new Version(BitPacker.GetString(data, p));
            this.m_PublicRSAKey = BitPacker.GetBytes(data, p);
            this.m_ConnectionKeySize = BitPacker.GetInt(data, p);
            this.ServerName = BitPacker.GetString(data, p);
            this.MaxPlayers = BitPacker.GetInt(data, p);
            this.CurrentPlayers = BitPacker.GetInt(data, p);
            //Log.LogMsg("Deserialized Rijndael Request." + PublicRSAKey);
            return true;
        }

        public override byte[] Serialize(Pointer p)
        {
            base.Serialize(p);
            BitPacker.AddString(ref m_SerializeBuffer, p, m_ServerVersion.ToString());
            BitPacker.AddBytes(ref m_SerializeBuffer, p, m_PublicRSAKey);
            BitPacker.AddInt(ref m_SerializeBuffer, p, m_ConnectionKeySize);
            BitPacker.AddString(ref m_SerializeBuffer, p, ServerName);
            BitPacker.AddInt(ref m_SerializeBuffer, p, MaxPlayers);
            BitPacker.AddInt(ref m_SerializeBuffer, p, CurrentPlayers);
            return m_SerializeBuffer;
        }
    }
}
