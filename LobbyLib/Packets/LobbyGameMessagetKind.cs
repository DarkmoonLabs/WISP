using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared
{
    public enum LobbyGameMessageSubType : byte
    {
        GameInfoNotification = 1,
        GamePropertiesUpdateNotification = 2,
        Chat = 3,
        NewOwner = 4,
        SeatChangeRequest = 5,
        // Player client sends this message AFTER the game has started AND they have loaded all of the level geometry.
        ClientLevelLoaded = 6, 
    }
}
