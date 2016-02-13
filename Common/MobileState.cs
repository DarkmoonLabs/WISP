using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared
{
    public class MobileState
    {
        public MobileState()
        {
            Position = new SVector3();
            Rotation = new SVector3();
            Velocity = new SVector3();
        }

        /// <summary>
        /// Current position
        /// </summary>
        public SVector3 Position = new SVector3();

        /// <summary>
        /// Rotation of object
        /// </summary>
        public SVector3 Rotation { get; set; }

        /// <summary>
        /// Velocity for each axis
        /// </summary>
        public SVector3 Velocity { get; set; }

        /// <summary>
        /// NetworkClock Time of this state
        /// </summary>
        public long TimeStamp { get; set; }
        

    }
}
