using System;
using System.Collections.Generic;
using System.Text;

namespace Shared
{
    /// <summary>
    /// Sent from cluster server A to cluste server B, when cluster server A has accepted the transfer of a player
    /// at the request of Cluster server B
    /// </summary>
    public class PacketPlayerAuthorizedForTransfer : PacketReply
    {
        public PacketPlayerAuthorizedForTransfer()
            : base()
        {
            Profile = new AccountProfile("-");
        }       

        public Guid Player { get; set; }
        public string AccountName { get; set; }
        public Guid AuthTicket { get; set; }
        public Guid TargetResource { get; set; }
        public AccountProfile Profile { get; set; }

        public override bool DeSerialize(byte[] data, Pointer p)
        {
            base.DeSerialize(data, p);
            Profile = (AccountProfile) BitPacker.GetSerializableWispObject(data, p);
            TargetResource = new Guid(BitPacker.GetString(data, p));
            Player = new Guid(BitPacker.GetString(data, p));
            AuthTicket = new Guid(BitPacker.GetString(data, p));
            AccountName = BitPacker.GetString(data, p);

            return true;
        }

        public override byte[] Serialize(Pointer p)
        {
            base.Serialize(p);
            BitPacker.AddSerializableWispObject(ref m_SerializeBuffer, p, Profile);
            BitPacker.AddString(ref m_SerializeBuffer, p, TargetResource.ToString());
            BitPacker.AddString(ref m_SerializeBuffer, p, Player.ToString());
            BitPacker.AddString(ref m_SerializeBuffer, p, AuthTicket.ToString());
            BitPacker.AddString(ref m_SerializeBuffer, p, AccountName);

            return m_SerializeBuffer;
        }
        
    }
}
