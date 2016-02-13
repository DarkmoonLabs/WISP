using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared
{
    public interface IGameAbortStrategy
    {
        GameServerGame OwningGame { get; set; }        
        void NotifyPlayerAddedToGame(ICharacterInfo toon);
        void NotifyPlayerLeftGame(int toon);
    }
}
