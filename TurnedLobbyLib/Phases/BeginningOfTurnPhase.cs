using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared
{
    public class BeginningOfTurnPhase : Phase
    {
        public BeginningOfTurnPhase(int responseTimeMs)
            : base(responseTimeMs)
        {
            this.PhaseName = "the beginning of the turn";
            this.PhaseID = (int)PhaseId.BeginTurn;
        }

        public override void OnBecameCurrent()
        {
            base.OnBecameCurrent();            
            Sequencer.OwningGame.OnBeforeNextTurnPhase(this); // "Before the beginning of the turn". notify players if there's a response delay            
        }

        public override bool TryExecuteEffect(ref string msg)
        {
            bool rslt = base.TryExecuteEffect(ref msg);

            if (!rslt)
            {
                return false;
            }

            //Sequencer.OwningGame.OnNextTurnPhase(this);
            //EndPhase();
            return rslt;
        }

        public override void EndPhase()
        {
            base.EndPhase();
            try
            {
                if (NextPhase == null)
                {
                    NextPhase = new MainPhase(0);
                }

                Sequencer.OwningGame.EndCurrentTurnPhase(NextPhase);                
            }
            catch
            {
                Log.LogMsg("Tried to end phase [" + PhaseName + "], but phase object not currently attached to a game.");
            }
        }

    }
}