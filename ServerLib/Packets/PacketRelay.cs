using System;
using System.Collections.Generic;
using System.Text;

namespace Shared
{
    /// <summary>
    /// Relays a packet to a user on a different server
    /// </summary>
    public class PacketRelay : Packet
    {
        public PacketRelay()
        {
            OriginServer = "";
            TargetServer = "";
            From = new CharacterInfo(-1);
            To = Guid.Empty;
            Message = new byte[0];
        }

        /// <summary>
        /// Originating server
        /// </summary>
        public string OriginServer { get; set; }

        /// <summary>
        /// Target Server
        /// </summary>
        public string TargetServer { get; set; }

        /// <summary>
        /// Message From user
        /// </summary>
        public CharacterInfo From { get; set; }

        /// <summary>
        /// To user.  Only have Guid in this packet... CharacterInfo persumably loaded at destination.
        /// </summary>
        public Guid To { get; set; }

        /// <summary>
        /// The message to send
        /// </summary>
        public byte[] Message { get; set; }

        public override bool DeSerialize(byte[] data, Pointer p)
        {             
            base.DeSerialize(data, p);
            OriginServer = BitPacker.GetString(data, p);
            TargetServer = BitPacker.GetString(data, p);
            To = new Guid(BitPacker.GetString(data, p));
            From = BitPacker.GetSerializableWispObject(data, p) as CharacterInfo;
            Message = BitPacker.GetBytes(data, p);
            return true;
        }

        public override byte[] Serialize(Pointer p)
        {
            base.Serialize(p);
            BitPacker.AddString(ref m_SerializeBuffer, p, OriginServer);
            BitPacker.AddString(ref m_SerializeBuffer, p, TargetServer);
            BitPacker.AddString(ref m_SerializeBuffer, p, To.ToString());
            BitPacker.AddSerializableWispObject(ref m_SerializeBuffer, p, From);
            BitPacker.AddBytes(ref m_SerializeBuffer, p, Message);
            return m_SerializeBuffer;
        }
    }
}
