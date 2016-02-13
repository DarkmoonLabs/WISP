using System;
using System.Collections.Generic;
using System.Text;

namespace PatcherLib
{
    public class OperatingSystemInfo : MachineInfoData
    {
        public OperatingSystemInfo()
        {
            PropertyName = "Win32_OperatingSystem";
        }

        /// <summary>
        /// Operating System name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// OS build number
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Service pack number
        /// </summary>
        public string ServicePack { get; set; }

        /// <summary>
        /// Extra info ("Server R2" for example)
        /// </summary>
        public string AdditionalInfo { get; set; }

        /// <summary>
        /// 32 or 64 bit
        /// </summary>
        public string Architecture { get; set; }

        /// <summary>
        /// OS Language
        /// </summary>
        public string Language { get; set; }

        /// <summary>
        /// Total amount, in megabytes, of physical memory available to the operating system. 
        /// This value does not necessarily indicate the true amount of physical memory, but what 
        /// is reported to the operating system as available to it.
        /// </summary>
        public float VisibleMemory { get; set; }

        public override string ToString()
        {
            return PropertyName + ":Name:" + Name + "|Ver:" + Version + "|ServicePack:" + ServicePack + "|AddtlInfo: " + AdditionalInfo + "|Architechture:" + Architecture + "|Lang:" + Language + "|VisibleMemory: " + VisibleMemory.ToString();
        }
    }
}
