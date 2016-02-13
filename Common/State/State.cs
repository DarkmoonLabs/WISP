using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Shared
{
    /// <summary>
    /// Games stuff happen in phases...i.e. they shift from one state to another.  This class
    /// encapsulates rules and functionality having to do with the current state that something is in
    /// </summary>
    public abstract class State : IState
    {
        public enum StateType : int
        {
            Default = -1
        }

        private State()
        {
        }

        protected State(string name)
        {
            m_StateName = name;
        }

        /// <summary>
        /// State identifier
        /// </summary>
        public int Kind { get; set; }

        protected string m_StateName = "NullState";
        /// <summary>
        /// Friendly name of the state
        /// </summary>
        public string StateName
        {
            get
            {
                return m_StateName;
            }
        }


        /// <summary>
        /// What happens when the state is first entered
        /// </summary>
        public virtual void Enter(IStatefulEntity ent)
        {
        }

        /// <summary>
        /// What happens when the state is exited (i.e. a new state is entered)
        /// </summary>
        public virtual void Exit(IStatefulEntity ent)
        {
        }

        /// <summary>
        /// Something that happens on demand, i.e. whenever this state is executed
        /// </summary>
        /// <param name="ent"></param>
        public virtual void ExecuteStateInstructions(IStatefulEntity ent)
        {
        }


    }
}