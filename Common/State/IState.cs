using System;
namespace Shared
{
    public interface IState
    {
        void Enter(global::Shared.IStatefulEntity ent);
        void ExecuteStateInstructions(global::Shared.IStatefulEntity ent);
        void Exit(global::Shared.IStatefulEntity ent);
        int Kind { get; set; }
        string StateName { get; }
    }
}
