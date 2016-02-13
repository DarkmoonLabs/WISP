using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace Shared
{
    public class NetQItem
    {
        public PacketFlags Flags;
        public byte[] Data;
        public bool IsUDP;
    }

}
