using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared
{
    public class PacketGameMessage : PacketReply
    {
        public PacketGameMessage()
            : base()
        {
            PacketTypeID = (int)LobbyPacketType.GameMessage;
            PacketSubTypeID = 0;
        }

        /// <summary>
        /// Another ID that can optionally be used to further ID a packet
        /// </summary>
        public int GameMessageKind { get; set; }

        public override bool DeSerialize(byte[] data, Pointer p)
        {
            base.DeSerialize(data, p);

            GameMessageKind = BitPacker.GetInt(data, p);
            return true;
        }

        public override byte[] Serialize(Pointer p)
        {
            base.Serialize(p);

            BitPacker.AddInt(ref m_SerializeBuffer, p, (GameMessageKind));
            return m_SerializeBuffer;
        }
    }
}
