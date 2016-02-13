using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared
{

    public class MainPhase : Phase
    {
        public MainPhase(int responseTimeMs)
            : base(responseTimeMs)
        {
            this.PhaseName = "the main phase of the turn";
            this.PhaseID = (int)PhaseId.Main;
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
            // no action. we linger in the main phase for all main play action. player will end this phase manually by calling EndPhase
            //EndPhase(); // debug
            return rslt;
        }

        public override void PlayerDone(ICharacterInfo player)
        {
            base.PlayerDone(player);
            EndPhase();
        }

        public override void EndPhase()
        {
            base.EndPhase();
            try
            {
                if (NextPhase == null)
                {
                    NextPhase = new EndOfTurnPhase(0);
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

