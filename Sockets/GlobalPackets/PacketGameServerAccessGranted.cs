using System;
using System.Collections.Generic;
using System.Text;

namespace Shared
{
    /// <summary>
    /// This packet is returned from the server cluster and lets us know if we are allowed to log in
    /// </summary>
    public class PacketGameServerTransferResult : PacketReply
    {
        public PacketGameServerTransferResult()
            : base()
        {
            AuthTicket = Guid.Empty;
            ServerName = "";
            ServerIP = "";
            ServerPort = -1;
            IsAssistedTransfer = true;
        }

        public Guid AuthTicket { get; set; }
        public string ServerName { get; set; }
        public string ServerIP { get; set; }
        public int ServerPort { get; set; }
        public Guid TargetResource { get; set; }
        public bool IsAssistedTransfer { get; set; }

        public override bool DeSerialize(byte[] data, Pointer p)
        {
            base.DeSerialize(data, p);
            AuthTicket = new Guid(BitPacker.GetString(data, p));
            ServerName = BitPacker.GetString(data, p);
            ServerIP = BitPacker.GetString(data, p);
            ServerPort = BitPacker.GetInt(data, p);
            TargetResource = new Guid(BitPacker.GetString(data, p));
            IsAssistedTransfer = BitPacker.GetBool(data, p);
            return true;
        }

        public override byte[] Serialize(Pointer p)
        {
            base.Serialize(p);
            BitPacker.AddString(ref m_SerializeBuffer, p, AuthTicket.ToString());
            BitPacker.AddString(ref m_SerializeBuffer, p, ServerName);
            BitPacker.AddString(ref m_SerializeBuffer, p, ServerIP);
            BitPacker.AddInt(ref m_SerializeBuffer, p, ServerPort);
            BitPacker.AddString(ref m_SerializeBuffer, p, TargetResource.ToString());
            BitPacker.AddBool(ref m_SerializeBuffer, p, IsAssistedTransfer);
            return m_SerializeBuffer;
        }
    }
}
