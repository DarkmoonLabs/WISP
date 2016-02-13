using System;
using System.Collections.Generic;
using System.Text;

namespace Shared
{
    /// <summary>
    /// Sent in reply to a request for the current status of the server
    /// </summary>
    public class PacketServerUpdate : PacketReply
    {
        public PacketServerUpdate()
        {
        }

        /// <summary>
        /// The name of the server
        /// </summary>
        public string ServerName { get; set; }

        /// <summary>
        /// The user ID of the server
        /// </summary>
        public string UserID { get; set; }

        /// <summary>
        /// Maximum number of players that the server will accept
        /// </summary>
        public int MaxPlayers { get; set; }
        
        /// <summary>
        /// The current number of players on that server
        /// </summary>
        public int CurrentPlayers { get; set; }

        public override bool DeSerialize(byte[] data, Pointer p)
        {
            base.DeSerialize(data, p);
            ServerName = BitPacker.GetString(data, p);
            MaxPlayers = BitPacker.GetInt(data, p);
            CurrentPlayers = BitPacker.GetInt(data, p);
            UserID = BitPacker.GetString(data, p);
            return true;
        }

        public override byte[] Serialize(Pointer p)
        {
            base.Serialize(p);
            byte[] serverName = System.Text.Encoding.UTF8.GetBytes(ServerName);
            BitPacker.AddString(ref m_SerializeBuffer, p, ServerName);
            BitPacker.AddInt(ref m_SerializeBuffer, p, MaxPlayers);
            BitPacker.AddInt(ref m_SerializeBuffer, p, CurrentPlayers);
            BitPacker.AddString(ref m_SerializeBuffer, p, UserID);
            return m_SerializeBuffer;
        }
    }
}
