using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared
{
    public abstract class Phase : GameSequencerItem, IPhase
    {
        // "[Before] the [beginning of your turn]"
        // "[At] the [end of the round]"
        // "[At] the [beginning of your turn]"
        // "[Before] the [end of your turn]"
        // "[Before] you [delcare attackers]"
        // "[During] your main phase you [may draw a card]."

        // [Before] == OnBecamseCurrent->OwningGame.OnBeforeNextPhase
        // [At] == TryExecuteEffect-OwningGame.OnNextPhase; IsActive = true
        // [During] == IsActive = true

        // [beginning of your turn], [end of the round], etc == individual Phases

        // EndPhase, signals moving on to next phase

        // Phases are divided into stages - Before/At/During.  Players may submit responses to the [Before] stage if the ResponseTime > 0. 
        // Players may always submit responses for the [During] stage, which the phase is in as a result of TryExecuteEffect (unless the [At] stage
        // specifically ends the phase, as may be the case for a phase like "EndOfTurnPhase", where having a lingering [During] state, doesn't make
        // sense.

        public Phase(int responseTimeMs) : base(responseTimeMs)
        {
            AllowInputFrom = new List<int>();
        }

        /// <summary>
        /// Is this phase currently active?
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// A list of character IDs of all players that can currently
        /// submit input.  When a player sends a command to the game
        /// we check it against this list.
        /// </summary>
        public List<int> AllowInputFrom { get; set; }

        public int PhaseID
        {
            get
            {
                return m_PhaseID;
            }
            set
            {
                m_PhaseID = value;
            }
        }
        private int m_PhaseID;

        public string PhaseName
        {
            get
            {
                return m_PhaseName;
            }
            set
            {
                m_PhaseName = value;
            }
        }
        private string m_PhaseName;

        public virtual void PlayerDone(ICharacterInfo player)
        {
        }

        public virtual bool CanPlayerSubmitCommand(ICharacterInfo player)
        {
            bool rslt = Sequencer.OwningGame.Players.Values.FirstOrDefault(p => p.ID == player.ID) != null;
            return rslt;
        }

        /// <summary>
        /// Helper method that populates the AllowInputFrom property with only the current player (or nothing, if there is
        /// not current player).
        /// </summary>
        protected void OnlyAllowInputFromCurrentPlayer()
        {
            AllowInputFrom.Clear();
            if (Sequencer.OwningGame.CurrentPlayer != null)
            {
                AllowInputFrom.Add(Sequencer.OwningGame.CurrentPlayer.ID);
            }
        }

        public override void OnBecameCurrent()
        {
            base.OnBecameCurrent();
            OnlyAllowInputFromCurrentPlayer();                     
        }

        /// <summary>
        /// This method is usually called by the GameSequencer.  It will attempt to execute the phase work items specified in OnPhaseExecute.      
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public override bool TryExecuteEffect(ref string msg)
        {
            if(!base.TryExecuteEffect(ref msg))
            {
                return false;
            }

            if (Sequencer.OwningGame == null)
            {
                msg = "Phase object not owned by any game.";
                return false;
            }

            IsActive = true;
            return true;
        }

        public virtual void EndPhase()
        {
            IsActive = false;
        }
      
        /// <summary>
        /// The phase we go to on completion of this phase
        /// </summary>
        public IPhase NextPhase { get; set; }

        public override void Serialize(ref byte[] buffer, Pointer p)
        {
            base.Serialize(ref buffer, p);
            BitPacker.AddInt(ref buffer, p, PhaseID);
            BitPacker.AddString(ref buffer, p, PhaseName);
            BitPacker.AddLong(ref buffer, p, ResponseTime);
        }

        public override void Deserialize(byte[] data, Pointer p)
        {
            base.Deserialize(data, p);
            PhaseID = BitPacker.GetInt(data, p);
            PhaseName = BitPacker.GetString(data, p);
            ResponseTime = BitPacker.GetLong(data, p);
        }
    }
}
