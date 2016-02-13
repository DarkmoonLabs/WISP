using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared
{
    public class RoundStartupSequenceItem : Phase
    {
        public RoundStartupSequenceItem(int responseTimeMs)
            : base(responseTimeMs)
        {
            this.PhaseName = "the beginning of the round";
            this.PhaseID = (int)PhaseId.RoundStartup;
        }

        public override void OnBecameCurrent()
        {
            Log.LogMsg("Before the beginning of round [" + (Sequencer.OwningGame.RoundNumber + 1) + "].");
            base.OnBecameCurrent();
            Sequencer.OwningGame.OnBeforeNextRound(this); // send note to players if there is a timer
        }

        public override bool TryExecuteEffect(ref string msg)
        {
            bool rslt = base.TryExecuteEffect(ref msg);

            if (!rslt)
            {
                return false;
            }
            Log.LogMsg("Round [" + (Sequencer.OwningGame.RoundNumber + 1) +"] has begun.");
            //EndPhase(); // we do not linger in the "before the beginning of the round" phase.
            return rslt;
        }

        public override void EndPhase()
        {
            base.EndPhase();
            try
            {
                Sequencer.OwningGame.OnNextRoundBegan(); // the "before the begining of the round" phase is followed by the actual beginning of the round actions, i.e. the start of the first player's turn
            }
            catch
            {
                Log.LogMsg("Tried to end phase [" + PhaseName + "], but phase object not currently attached to a game.");
            }
        }

    }
}
