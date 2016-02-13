using System;
using System.Collections.Generic;
namespace Shared
{
    public interface IGame : IPropertyBagOwner, IComponent
    {
        List<ICharacterInfo> AllPlayers { get; set; }
        List<ICharacterInfo> AllObservers { get; set; }
        Dictionary<int, ICharacterInfo> Observers { get; set; }
        bool Ended { get; set; }
        Guid GameID { get; set; }
        bool IsShuttingDown { get; set; }
        int MaxPlayers { get; set; }
        int MaxObservers { get; set; }
        int Owner { get; set; }
        Dictionary<int, ICharacterInfo> Players { get; set; }
        Dictionary<int, ICharacterInfo> EverActivePlayers { get; set; }
        int[] Quitters { get; }
        Shared.PropertyBag Properties { get; set; }
        bool Solved { get; set; }
        bool Started { get; set; }
        object AllPlayersSyncRoot {get;}
        object AllObserversSyncRoot { get; }
        bool IsPlayerPartOfGame(int id);
        bool IsPlayerObserver(int id);
        GameState CurrentGameState { get; set; }
        string Name { get; set; }
    }
}
