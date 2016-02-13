using System;
using System.Collections.Generic;
using System.Text;

namespace PatcherLib
{
    public class CPUInfo : MachineInfoData
    {
        public CPUInfo()
        {
            PropertyName = "Win32_Processor";
        }

        /// <summary>
        /// CPU Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Number of cores for the current instance of the processor. A core is a physical processor on the integrated circuit. For example, in a dual-core processor this property has a value of 2.
        /// For example, a dual-processor system that contains two processors enabled for hyperthreading can run four threads or programs or simultaneously. In this case, NumberOfCores is 2 and NumberOfLogicalProcessors is 4.
        /// </summary>
        public int NumberOfCores { get; set; }

        /// <summary>
        /// Number of logical processors for the current instance of the processor. For processors capable of hyperthreading, this value includes only the processors which have hyperthreading enabled. 
        /// For example, a dual-processor system that contains two processors enabled for hyperthreading can run four threads or programs or simultaneously. In this case, NumberOfCores is 2 and NumberOfLogicalProcessors is 4.
        /// </summary>
        public int NumberOfLogicalProcessors { get; set; }

        /// <summary>
        /// Maximum speed of the processor, in MHz.
        /// </summary>
        public uint MaxClockSpeed { get; set; }

        public override string ToString()
        {
            return this.PropertyName + ":Name:" + Name + "|Cores:" + NumberOfCores + "|LogicalProcs:" + NumberOfLogicalProcessors + "|Speed:" + MaxClockSpeed;
        }
    }
}
