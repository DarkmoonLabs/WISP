using System;
namespace Shared
{
    public interface IGameSequencerItem
    {
        int ResponseTimeout { get; }
        long ResponseTime { get; }
        GameSequencer Sequencer { get; set; }
        bool TryExecuteEffect(ref string msg);
        void OnBecameCurrent();
        void OnBecameNotCurrent();
        bool HasBegunExecution { get; set; }
        int ResponseTimerMod { get; set; }
    }
}
