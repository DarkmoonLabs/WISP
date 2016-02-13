using System;
using System.Collections.Generic;
using System.Text;

namespace Shared
{
    public class StateDefault : State
    {
        protected StateDefault(string name) : base(name)
        {
            this.Kind = (int)StateType.Default;
        }

        private static StateDefault m_Instance;
        public static StateDefault Instance
        {
            get
            {
                if(m_Instance == null)
                {
                    m_Instance = new StateDefault("Default State");
                }

                return m_Instance;
            }
        }

        public override void Enter(IStatefulEntity ent)
        {
            //System.Diagnostics.Debug.WriteLine("Default state entered.");
        }

        public override void Exit(IStatefulEntity ent)
        {
            //System.Diagnostics.Debug.WriteLine("Default state exited.");
        }

        public override void ExecuteStateInstructions(IStatefulEntity ent)
        {
            //System.Diagnostics.Debug.WriteLine("Executing default state instructions (nothing).");
        }
    }
}
