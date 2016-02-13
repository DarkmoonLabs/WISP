using System;
using System.Collections.Generic;
using System.Text;

namespace PatcherLib
{
    public class GPUInfo
    {
        /// <summary>
        /// Name of the card
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Driver version for this GPU
        /// </summary>
        public string DriverVersion { get; set; }

        /// <summary>
        /// The amount of RAM available on the card, in megabytes
        /// </summary>
        public float AdapterRam { get; set; }
    }
}
