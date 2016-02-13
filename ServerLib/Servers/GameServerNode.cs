using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace Shared
{
    /// <summary>
    /// A game server node handles the connection of an incoming CentralServer. Subclass and handle game specific logic in this object.
    /// </summary>
    public class GameServerNode : ServerBase
    {
        public override void StartServer()
        {
            if (ConfigHelper.GetStringConfig("SupressCharacterLoad", "FALSE").ToLower() == "false")
            {
                CharacterUtil.Instance.CharacterMinNameLength = ConfigHelper.GetIntConfig("CharacterMinNameLength", 3);
                CharacterUtil.Instance.LoadCharacterTemplate();
            }
            base.StartServer();
        }

    }
}
