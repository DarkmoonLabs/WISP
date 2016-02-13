using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared
{
    public interface IGameStartStrategy
    {
        GameServerGame OwningGame { get; set; }
        bool CanGameBeStartedManually { get; set; }
        bool DoesGameMeetStartConditions { get; }
        void NotifyPlayerAddedToLobby(ICharacterInfo toon);
        void NotifyPlayerLeftLobby(int toon);

    }
}
