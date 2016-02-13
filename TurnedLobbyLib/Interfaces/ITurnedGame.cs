using System;
using Shared;
using System.Collections.Generic;

namespace Shared
{
    public interface ITurnedGame : IGame
    {
        ICharacterInfo CurrentPlayer { get; set; }
        void EndCurrentTurnPhase(IPhase nextPhase);
        void EndPlayersTurn();
        GameSequencer GamePhaseSequencer { get; set; }
        void NextPlayersTurn();
        void OnBeforeNextRound(RoundStartupSequenceItem item);
        void OnBeforeNextTurnPhase(IPhase itm);
        void OnNextRoundBegan();
        void OnNextTurnPhase(IPhase itm);
        void OnRoundEnded();
        List<int> PlayerTurnOrder { get; set; }
        int RoundNumber { get; set; }

        void OnBeforeRoundEnd(RoundEndSequenceItem item);
    }
}
