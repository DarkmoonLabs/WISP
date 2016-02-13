using System;
using System.Collections.Generic;
using System.Text;

namespace Shared
{
    /// <summary>
    /// A textual message that appears for all players.
    /// </summary>
    public class PacketGameInfoNotification : PacketGameMessage
    {
        public PacketGameInfoNotification()
        {
            PacketSubTypeID = (int)LobbyGameMessageSubType.GameInfoNotification;
        }

        /// <summary>
        /// The message to display
        /// </summary>
        public string Message { get; set; }

        public override bool DeSerialize(byte[] data, Pointer p)
        {
            base.DeSerialize(data, p);
            Message = BitPacker.GetString(data, p);
            return true;
        }

        public override byte[] Serialize(Pointer p)
        {
            base.Serialize(p);
            BitPacker.AddString(ref m_SerializeBuffer, p, Message);
            return m_SerializeBuffer;
        }
    }
}
