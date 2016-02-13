using System;
using System.Collections.Generic;
using System.Text;

namespace Shared
{
    /// <summary>
    /// Helper class that keeps track of array positions when reading packed binary packet data.
    /// </summary>
    public class Pointer
    {
        public Pointer()
        {
            Position = 0;
        }

        public void Reset()
        {
            Position = 0;
        }

        /// <summary>
        /// The current position of the pointer
        /// </summary>
        public int Position { get; set; }

        /// <summary>
        /// Advances the pointer by some amount
        /// </summary>
        /// <param name="len">the number to advance the pointer by</param>
        /// <returns>the position of the pointer BEFORE @len is added to it</returns>
        public int Advance(int len)
        {
            Position += len;
            return Position - len;
        }
    }
}
