using System;
using System.Collections.Generic;
using System.Text;

namespace Shared
{
    public class PacketACK : PacketReply
    {
        /// <summary>
        /// Acknowledgement packet.  Contains no data.
        /// </summary>
        public PacketACK() : base()
        {
        }
    }
}
