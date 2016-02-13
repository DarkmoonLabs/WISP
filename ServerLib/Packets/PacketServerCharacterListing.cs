using System;
using System.Collections.Generic;
using System.Text;

namespace Shared
{
    /// <summary>
    /// A listing of all characters currently being hosted on a server
    /// </summary>
    public class PacketServerCharacterListing : PacketReply
    {
        public PacketServerCharacterListing()
        {
            Characters = new List<ServerCharacterInfo>();
        }

        /// <summary>
        /// The list of characters
        /// </summary>
        public List<ServerCharacterInfo> Characters { get; set; }

        public override bool DeSerialize(byte[] data, Pointer p)
        {             
            base.DeSerialize(data, p);

            int num = BitPacker.GetInt(data, p);
            for (int i = 0; i < num; i++)
            {
                Characters.Add(BitPacker.GetComponent(data, p) as ServerCharacterInfo);
            }

            return true;
        }

        public override byte[] Serialize(Pointer p)
        {
            base.Serialize(p);
            BitPacker.AddInt(ref m_SerializeBuffer, p, Characters.Count);
            for (int i = 0; i < Characters.Count; i++)
            {
                BitPacker.AddComponent(ref m_SerializeBuffer, p, Characters[i]);
            }
            return m_SerializeBuffer;
        }
    }
}
