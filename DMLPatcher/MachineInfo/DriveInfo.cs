using System;
using System.Collections.Generic;
using System.Text;

namespace PatcherLib
{
    public class DriveInfo : MachineInfoData
    {
        public DriveInfo()
        {
            PropertyName = "Win32_LogicalDisk";
            Drives = new List<DiskInfo>();
        }

        /// <summary>
        /// All of the logical drives on this machine
        /// </summary>
        public List<DiskInfo> Drives { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(PropertyName + ":NumDrives:" + Drives.Count.ToString());
            for (int i = 0; i < Drives.Count; i++)
            {
                sb.Append("||Drive" + i.ToString() + ": ");
                sb.Append("ID:" + Drives[i].DeviceID + "|");
                sb.Append("Type:" + Drives[i].DriveType + "|");
                sb.Append("FileSystem:" + Drives[i].FileSystem + "|");
                sb.Append("FreeSpaceMB:" + Drives[i].FreeSpace.ToString() + "|");
                sb.Append("SizeMB:" + Drives[i].Size.ToString() + "|");
            }

            return sb.ToString();
        }
    }
}
