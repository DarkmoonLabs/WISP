using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared
{
    /// <summary>
    /// A finite state machine.
    /// </summary>
   public class StateMachine
    {
        private IStatefulEntity m_Owner         = default(IStatefulEntity);
        private IState m_CurrentState = StateDefault.Instance;
        private IState m_PreviousState = StateDefault.Instance;
        private IState m_GlobalState = StateDefault.Instance;        

        private StateMachine()
        {
        }

        public IStatefulEntity Owner
        {
            get
            {
                return m_Owner;
            }
            set
            {
                m_Owner = value;
            }
        }

       /// <summary>
       /// The current state that the machine is in
       /// </summary>
        public IState CurrentState
        {
            get
            {
                return m_CurrentState;
            }
            set
            {
                ChangeState(value);
            }
        }

       /// <summary>
       /// The previous state of the machone
       /// </summary>
        public IState PreviousState
        {
            get
            {
                return m_PreviousState;
            }
        }

       /// <summary>
       /// A global state, which the machine is always in
       /// </summary>
        public IState GlobalState
        {
            get
            {
                return m_GlobalState;
            }
        }

        public StateMachine(IStatefulEntity owner, IState previousState, IState globalState)
        {
            m_Owner = owner;
            m_PreviousState = previousState;
            m_GlobalState = globalState;
        }

        /// <summary>
        /// Main method of the state machine.  Call every frame (or whatever interval) to cause the AI to "think"
        /// </summary>
        public virtual void ExecuteStateInstructions()
        {
#if DEBUG
            DateTime start = DateTime.UtcNow;            
#endif
            if(m_CurrentState != null)
            {
                m_CurrentState.ExecuteStateInstructions(m_Owner);
            }

            if(m_GlobalState != null)
            {
                m_GlobalState.ExecuteStateInstructions(m_Owner);
            }

#if DEBUG
            DateTime end = DateTime.UtcNow;
            TimeSpan ts = end - start;
            //System.Diagnostics.Debug.WriteLine(m_Owner.ToString() + " executed state for " + ts.TotalMilliseconds + " ms");
#endif
        }

       /// <summary>
       /// Pushes the machine into a new state and retains the current state as previous state.
       /// </summary>
       /// <param name="newState"></param>
        public void ChangeState(IState newState)
        {
            if(newState == null)
            {
                //System.Diagnostics.Debug.WriteLine("Attempted to set a null state for " + m_Owner.ToString());
#if DEBUG
                throw new Exception("Attempted to set a null state for " + m_Owner.ToString());
#endif
                return;
            }

            if (m_Owner.OnBeforeCurrentStateChanged(this, m_CurrentState, newState))
            {
                m_PreviousState = m_CurrentState;

                if (m_CurrentState != null)
                {
                    m_CurrentState.Exit(m_Owner);
                }
                m_CurrentState = newState;
                m_CurrentState.Enter(m_Owner);
                
                m_Owner.OnAfterCurrentStateChanged(this, m_PreviousState, m_CurrentState);
            }
        }

       /// <summary>
       /// Reverts to the state right before the current state
       /// </summary>
        public void RevertToPreviousState()
        {
            ChangeState(m_PreviousState);
        }

    }
}
