using System;
using System.Collections.Generic;
using System.Text;

namespace Shared
{
    /// <summary>
    /// A textual message that appears for all players.
    /// </summary>
    public class PacketPhaseUpdate : PacketGameMessage
    {
        public enum UpdateKind : byte
        {
            /// <summary>
            /// Notifies that the phase has come current but won't execute until a delay is expired. Player may be able to submit commands during delay.
            /// </summary>
            EnteredWithDelay,
            /// <summary>
            /// Item has executed, i.e. IsActive
            /// </summary>
            Entered,
        }

        public PacketPhaseUpdate()
        {
            PacketSubTypeID = (int)TurnedGameMessageSubType.PhaseUpdate;
        }

        /// <summary>
        /// The phase in question
        /// </summary>
        public Phase Phase { get; set; }

        /// <summary>
        /// What kind of update is this
        /// </summary>
        public UpdateKind PhaseUpdateKind { get; set; }

        public override bool DeSerialize(byte[] data, Pointer p)
        {
            base.DeSerialize(data, p);
            Phase = BitPacker.GetSerializableWispObject(data, p) as Phase;
            PhaseUpdateKind = (PacketPhaseUpdate.UpdateKind)BitPacker.GetByte(data, p);
            return true;
        }

        public override byte[] Serialize(Pointer p)
        {
            base.Serialize(p);
            BitPacker.AddSerializableWispObject(ref m_SerializeBuffer, p, Phase);
            BitPacker.AddByte(ref m_SerializeBuffer, p, (byte)PhaseUpdateKind);
            return m_SerializeBuffer;
        }
    }
}
