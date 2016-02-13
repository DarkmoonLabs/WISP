using System;
using System.Collections.Generic;
using System.Text;

namespace Shared
{
    public class GameServerInfo
    {
        public string Name { get; set; }
        public string IP { get; set; }
        public int MaxUsers { get; set; }
        public int CurUsers { get; set; }
        public DateTime LastUpdate { get; set; }
        public int Port { get; set; }
        public bool IsOnline { get; set; }

        public string UniqueID
        {
            get
            {
                return IP + ":" + Port;
            }
        }
    }
}
