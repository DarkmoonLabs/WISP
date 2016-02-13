using System;
using System.Collections.Generic;
using System.Text;

namespace PatcherLib
{
    public class VideoCardInfo : MachineInfoData
    {
        public VideoCardInfo()
        {
            PropertyName = "Win32_VideoController";
            VideoCards = new List<GPUInfo>();
        }

        /// <summary>
        /// All of the video cards on this system
        /// </summary>
        public List<GPUInfo> VideoCards { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(PropertyName + ":NumCards:" + VideoCards.Count.ToString());
            for (int i = 0; i < VideoCards.Count; i++)
            {
                sb.Append("||Card" + i.ToString() + ": ");
                sb.Append("Name:" + VideoCards[i].Name + "|");
                sb.Append("DriverVer:" + VideoCards[i].DriverVersion + "|");
                sb.Append("RAM:" + VideoCards[i].AdapterRam.ToString() + "|");
            }

            return sb.ToString();
        }
    }
}
