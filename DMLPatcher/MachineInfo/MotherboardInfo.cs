using System;
using System.Collections.Generic;
using System.Text;

namespace PatcherLib
{
    public class MotherboardInfo : MachineInfoData
    {
        public MotherboardInfo()
        {
            PropertyName = "Win32_ComputerSystemProduct";
        }

        /// <summary>
        /// Motherboard name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The vendor for this motherboard
        /// </summary>
        public string Vendor { get; set; }

        public override string ToString()
        {
            return PropertyName + ":Name: " + Name + "|" + Vendor;
        }
    }
}
