using System;
using System.Collections.Generic;
using System.Text;

namespace PatcherLib
{
    public class RamInfo : MachineInfoData
    {
        public RamInfo()
        {
            PropertyName = "Win32_MemoryDevice";
            Sizes = new List<float>();
        }

        /// <summary>
        /// The number of RAM devices (most likely sticks of RAM)
        /// </summary>
        public int NumberOfDevice { get; set; }

        /// <summary>
        /// Megabytes of storage in each device
        /// </summary>
        public List<float> Sizes { get; set; }
        
        /// <summary>
        /// Total amount of system RAM installed
        /// </summary>
        public float TotalRAM { get; set; }

        public override string ToString()
        {
            string sizes = "";
            foreach (float f in Sizes)
            {
                sizes += f.ToString() + ",";
            }
            sizes.TrimEnd(char.Parse(","));
            return PropertyName + ":NumDevices: " + NumberOfDevice.ToString() + "|MBinEachDevice:" + sizes + "|TotalRam: " + TotalRAM.ToString();
        }
    }
}
