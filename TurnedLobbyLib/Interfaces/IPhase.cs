using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared
{
    public interface IPhase
    {
        int PhaseID { get; set; }
        string PhaseName { get; set; }
        IPhase NextPhase { get; set; }
        void PlayerDone(ICharacterInfo player);
        bool CanPlayerSubmitCommand(ICharacterInfo player);
        List<int> AllowInputFrom { get; set; }
    }
}
