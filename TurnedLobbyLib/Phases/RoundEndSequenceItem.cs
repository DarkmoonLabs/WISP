using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared
{
    public class RoundEndSequenceItem : Phase
    {
        public RoundEndSequenceItem(int responseTimeMs)
            : base(responseTimeMs)
        {
            this.PhaseName = "the end of the round";
            this.PhaseID = (int)PhaseId.RoundEnd;
        }

        public override void OnBecameCurrent()
        {
            base.OnBecameCurrent();
            Sequencer.OwningGame.OnBeforeRoundEnd(this);
        }

        public override bool TryExecuteEffect(ref string msg)
        {
            bool rslt = base.TryExecuteEffect(ref msg);

            if (!rslt)
            {
                return false;
            }
            
            //EndPhase();
            return rslt;
        }

        public override void EndPhase()
        {
            base.EndPhase();
            try
            {
                Sequencer.OwningGame.OnRoundEnded();
            }
            catch
            {
                Log.LogMsg("Tried to end phase [" + PhaseName + "], but phase object not currently attached to a game.");
            }
        }
    }
}
