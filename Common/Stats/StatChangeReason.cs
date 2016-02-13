using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared
{
    public enum StatChangeReason
    {
        None,
        /// <summary>
        /// Part of the round update. used for "each turn" effects
        /// </summary>
        Turn,
        /// <summary>
        /// Stat change happened because of an effect going off
        /// </summary>
        Effect,

        /// <summary>
        /// Stat changed because of something that was purchased
        /// </summary>
        Purchase,

        /// <summary>
        /// Stat changed happened because I sold something
        /// </summary>
        Sale,

        /// <summary>
        /// Stat change happened as part of crafting
        /// </summary>
        Crafting
    }
}
