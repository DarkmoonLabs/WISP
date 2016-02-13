using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared
{
    public class EndOfTurnPhase : Phase
    {
        public EndOfTurnPhase(int responseTimeMs)
            : base(responseTimeMs)
        {
            this.PhaseName = "the end of the turn";
            this.PhaseID = (int)PhaseId.EndTurn;
        }

        public override void OnBecameCurrent()
        {
            base.OnBecameCurrent();
            Sequencer.OwningGame.OnBeforeNextTurnPhase(this); // "Before the main phase of the turn". notify players if there's a response delay
        }

        public override bool TryExecuteEffect(ref string msg)
        {
            bool rslt = base.TryExecuteEffect(ref msg);

            if (!rslt)
            {
                return false;
            }

            //Sequencer.OwningGame.OnNextTurnPhase(this);
            //EndPhase(); // we do not linger in the end-ofturn phase. 
            return rslt;
        }

        public override void EndPhase()
        {
            base.EndPhase();
            try
            {
                Sequencer.OwningGame.NextPlayersTurn();                
            }
            catch
            {
                Log.LogMsg("Tried to end phase [" + PhaseName + "], but phase object not currently attached to a game.");
            }
        }

    }
}