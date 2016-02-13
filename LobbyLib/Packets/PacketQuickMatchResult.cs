 using System;
using System.Collections.Generic;
using System.Text;

namespace Shared
{
    /// <summary>
    /// Multiple MatchNotifcations stuffed into one packet
    /// </summary>
    public class PacketMatchRefresh : PacketReply
    {
        public PacketMatchRefresh()
            : base()
        {
            TheGames = new List<Game>();
            Kinds = new List<MatchNotificationType>();
            TargetPlayers = new List<ICharacterInfo>();
            IsRefresh = false;
            IsServerPacket = false;
        }

        /// <summary>
        /// The games in question
        /// </summary>
        public List<Game> TheGames { get; set; }

        /// <summary>
        /// Some notifications revolve around a player, this is them, if applicable
        /// </summary>
        public List<ICharacterInfo> TargetPlayers { get; set; }

        /// <summary>
        /// The type of notification
        /// </summary>
        public List<MatchNotificationType> Kinds { get; set; }

        /// <summary>
        /// Is this a full refresh, i.e. listing,  of all known games?
        /// </summary>
        public bool IsRefresh { get; set; }

        /// <summary>
        /// Is this packet destined for a server?  otherwise it's meant for a player.
        /// Player packets contain slightly less data.
        /// </summary>
        public bool IsServerPacket { get; set; }

        public override bool DeSerialize(byte[] data, Pointer p)
        {
            base.DeSerialize(data, p);
            IsRefresh = BitPacker.GetBool(data, p);
            IsServerPacket = BitPacker.GetBool(data, p);
            int num = BitPacker.GetInt(data, p);
            for (int i = 0; i < num; i++)
            {
                if (!IsRefresh)
                {
                    MatchNotificationType kind = (MatchNotificationType)BitPacker.GetInt(data, p);
                    Kinds.Add(kind);
                }

                Game theGame = BitPacker.GetComponent(data, p, IsServerPacket) as Game;

                TheGames.Add(theGame);
                bool hasTargetPlayer = BitPacker.GetBool(data, p);
                if (!IsRefresh && hasTargetPlayer)
                {
                    TargetPlayers.Add(BitPacker.GetComponent(data, p, false) as ICharacterInfo);
                }
            }
            return true;
        }

        public override byte[] Serialize(Pointer p)
        {
            base.Serialize(p);
            BitPacker.AddBool(ref m_SerializeBuffer, p, IsRefresh);
            BitPacker.AddBool(ref m_SerializeBuffer, p, IsServerPacket);
            BitPacker.AddInt(ref m_SerializeBuffer, p, TheGames.Count);
            for (int i = 0; i < TheGames.Count; i++)
            {
                if (!IsRefresh)
                {
                    BitPacker.AddInt(ref m_SerializeBuffer, p, (int)Kinds[i]);
                }
            
                // determine if this packet goes to another server, in which case we need to send the full game data,
                // or to the client in which case we need to just send a matchInfo object
                BitPacker.AddComponent(ref m_SerializeBuffer, p, TheGames[i], IsServerPacket);

                BitPacker.AddBool(ref m_SerializeBuffer, p, TargetPlayers.Count > i);
                if (!IsRefresh && TargetPlayers[i] != null)
                {
                    BitPacker.AddComponent(ref m_SerializeBuffer, p, TargetPlayers[i], false);
                }
            }

            return m_SerializeBuffer;
        }
       
    }
}
