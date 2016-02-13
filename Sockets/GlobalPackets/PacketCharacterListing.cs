using System;
using System.Collections.Generic;
using System.Text;

namespace Shared
{
    /// <summary>
    /// A listing of all characters for an account
    /// </summary>
    public class PacketCharacterListing : PacketReply
    {
        public PacketCharacterListing()
        {
            Characters = new List<ICharacterInfo>();
            Flags &= PacketFlags.IsCompressed;
        }

        /// <summary>
        /// The list of characters on the account
        /// </summary>
        public List<ICharacterInfo> Characters { get; set; }

        public override bool DeSerialize(byte[] data, Pointer p)
        {             
            base.DeSerialize(data, p);

            int num = BitPacker.GetInt(data, p);
            for (int i = 0; i < num; i++)
            {
                Characters.Add(BitPacker.GetComponent(data, p, false) as ICharacterInfo);
            }

            return true;
        }

        public override byte[] Serialize(Pointer p)
        {
            base.Serialize(p);
            
            BitPacker.AddInt(ref m_SerializeBuffer, p, Characters.Count);
            for (int i = 0; i < Characters.Count; i++)
            {
                // this is just an overview packet.  don't send component details.
                BitPacker.AddComponent(ref m_SerializeBuffer, p, Characters[i], false);
            }
            return m_SerializeBuffer;
        }
    }
}
