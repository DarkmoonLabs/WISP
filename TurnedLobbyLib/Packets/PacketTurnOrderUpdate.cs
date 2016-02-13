using System;
using System.Collections.Generic;
using System.Text;

namespace Shared
{
    /// <summary>
    /// Lets clients know the new turn order of players.
    /// </summary>
    public class PacketTurnOrderUpdate : PacketGameMessage
    {
        public PacketTurnOrderUpdate()
        {
            PacketSubTypeID = (int)TurnedGameMessageSubType.TurnOrderUpdate;
        }

        /// <summary>
        /// The phase in question
        /// </summary>
        public List<int> CharacterIdOrder = new List<int>();

        public override bool DeSerialize(byte[] data, Pointer p)
        {
            base.DeSerialize(data, p);
            CharacterIdOrder = BitPacker.GetIntList(ref data, p);
            return true;
        }

        public override byte[] Serialize(Pointer p)
        {
            base.Serialize(p);
            BitPacker.AddIntList(ref m_SerializeBuffer, p, CharacterIdOrder);
            return m_SerializeBuffer;
        }
    }
}
