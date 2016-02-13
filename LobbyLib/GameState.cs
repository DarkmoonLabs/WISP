using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared
{
    public enum GameState
    {
        /// <summary>
        /// Game is in the lobby state, i.e. has not yet started.  Players are still gathering.
        /// </summary>
        Lobby,
        /// <summary>
        /// Game is no longer in the lobby and play has begun.
        /// </summary>
        Started
    }
}
