using System;
using System.Collections.Generic;
using System.Text;

namespace Shared
{
    public enum MatchNotificationType
    {
        None = -1,
        MatchCreated = 0,
        MatchStarted = 1,
        MatchEnded = 2,
        PlayerAdded = 3,
        PlayerRemoved = 4,
        ObserverAdded = 5,
        ObserverRemoved = 6,
        ListingRefresh = 7,
    }

    /// <summary>
    /// Encapsulates data about an update to one match/game/room/instance 
    /// </summary>
    public class PacketMatchNotification : PacketReply
    {       
        public PacketMatchNotification()
            : base()
        {
            TheGame = null;
            TargetPlayer = null;
            Kind = MatchNotificationType.None;
            TheGameID = Guid.Empty;
        }

        /// <summary>
        /// The game in question
        /// </summary>
        public IGame TheGame { get; set; }

        /// <summary>
        /// The game in question
        /// </summary>
        public Guid TheGameID { get; set; }

        /// <summary>
        /// The kind of match update
        /// </summary>
        public MatchNotificationType Kind { get; set; }

        /// <summary>
        /// Some notifications revolve around a specific character. This is that character's ID, if applicable.
        /// </summary>
        public ICharacterInfo TargetPlayer { get; set; }

        public override bool DeSerialize(byte[] data, Pointer p)
        {
            base.DeSerialize(data, p);
            Kind = (MatchNotificationType)BitPacker.GetInt(data, p);

            bool haveGame = BitPacker.GetBool(data, p);
            if (haveGame)
            {
                TheGame = BitPacker.GetComponent(data, p, false) as IGame;
            }
            
            bool haveTargetPlayer = BitPacker.GetBool(data, p);
            if (haveTargetPlayer)
            {
                TargetPlayer = BitPacker.GetComponent(data, p, false) as ICharacterInfo;
            }

            TheGameID = new Guid(BitPacker.GetString(data, p));
            return true;
        }

        public override byte[] Serialize(Pointer p)
        {
            base.Serialize(p);
            BitPacker.AddInt(ref m_SerializeBuffer, p, (int)Kind);

            if (TheGame == null)
            {
                BitPacker.AddBool(ref m_SerializeBuffer, p, false); // do not have game data
            }
            else
            {
                BitPacker.AddBool(ref m_SerializeBuffer, p, true); // have game data
                BitPacker.AddComponent(ref m_SerializeBuffer, p, TheGame, false);
            }

            BitPacker.AddBool(ref m_SerializeBuffer, p, TargetPlayer != null);
            if (TargetPlayer != null)
            {
                BitPacker.AddComponent(ref m_SerializeBuffer, p, TargetPlayer, false);
            }

            BitPacker.AddString(ref m_SerializeBuffer, p, TheGameID.ToString());

            return m_SerializeBuffer;
        }


    }
}
