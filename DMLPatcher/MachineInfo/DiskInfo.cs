using System;
using System.Collections.Generic;
using System.Text;

namespace PatcherLib
{
    public class DiskInfo
    {
        /// <summary>
        /// Drive Title
        /// </summary>
        public string DeviceID { get; set; }

        /// <summary>
        /// Drive type, i.e. removeable media, cd-rom etc
        /// </summary>
        public string DriveType { get; set; }

        /// <summary>
        /// file system, NTFS, etc
        /// </summary>
        public string FileSystem { get; set; }

        /// <summary>
        /// Free space on disk, in megabytes
        /// </summary>
        public float FreeSpace { get; set; }

        /// <summary>
        /// Total size of the disk, in megabytes
        /// </summary>
        public float Size { get; set; }
    }
}
