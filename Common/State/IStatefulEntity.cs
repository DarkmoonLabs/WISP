using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared
{
    /// <summary>
    /// A thing that can be in various states
    /// </summary>
    public interface IStatefulEntity
    {
        bool OnBeforeCurrentStateChanged(StateMachine sm, IState oldState, IState newState);
        void OnAfterCurrentStateChanged(StateMachine sm, IState previousState, IState currentState);
    }
}
