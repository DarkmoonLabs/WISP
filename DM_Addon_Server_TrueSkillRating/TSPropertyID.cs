using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared
{
    public enum TSPropertyID
    {
        /// <summary>
        /// The number of games a character has played. -500.
        /// </summary>
        GamesPlayed = -500,

        /// <summary>
        /// The mean rating of the character. AkA Mu. -499.
        /// </summary>
        RatingMean = -499,

        /// <summary>
        /// The standard deviation of the character's rating. AkA Sigma. -498.
        /// </summary>
        RatingStandardDeviation = -498
    }
}
