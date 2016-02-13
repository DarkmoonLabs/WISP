using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared
{
    public enum PhaseId : int
    {
        RoundStartup = -1000,
        RoundEnd = -999,
        BeginTurn = -998,
        EndTurn = -997,
        Main = -996,

    }
}
