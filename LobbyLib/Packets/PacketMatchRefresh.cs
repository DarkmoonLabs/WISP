 using System;
using System.Collections.Generic;
using System.Text;

namespace Shared
{
    /// <summary>
    /// QuickMatch result notification
    /// </summary>
    public class PacketQuickMatchResult : PacketReply
    {
        public PacketQuickMatchResult()
            : base()
        {
            TheGame = null;
            StillLooking = false;
        }

        /// <summary>
        /// The game that was matched
        /// </summary>
        public Game TheGame { get; set; }

        /// <summary>
        /// Is the server still looking for a match?  If TheGame is null and StillLooking is false, the server gave up finding a game.
        /// </summary>
        public bool StillLooking { get; set; }

        public override bool DeSerialize(byte[] data, Pointer p)
        {
            base.DeSerialize(data, p);
            bool haveGame = BitPacker.GetBool(data, p);
            if(haveGame)
            {
                TheGame = BitPacker.GetComponent(data, p, false) as Game;
            }
            else
            {
                TheGame = null;
            }

            StillLooking = BitPacker.GetBool(data, p);
            return true;
        }

        public override byte[] Serialize(Pointer p)
        {
            base.Serialize(p);

            BitPacker.AddBool(ref m_SerializeBuffer, p, TheGame != null);
            if (TheGame != null)
            {
                BitPacker.AddComponent(ref m_SerializeBuffer, p, TheGame, false);
            }
            BitPacker.AddBool(ref m_SerializeBuffer, p, StillLooking);

            return m_SerializeBuffer;
        }
       
    }
}
