using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PatcherLib
{
    /// <summary>
    /// Describes a changed file and how it was changed
    /// </summary>
    public class Diff
    {
        public Diff(DiffType kind, string targetFile)
        {
            Kind = kind;
            TargetFile = targetFile;
        }

        public string HashedFilename = "";        
        public DiffType Kind;
        public string TargetFile;
    }
    /// <summary>
    /// The type of difference that was recorded for a specific file.
    /// </summary>
    public enum DiffType
    {
        Add = 1,
        Delete = 0,
        Change = -1,
        /// <summary>
        /// If a patch file is larger than the file itself (can happen with an unsuitable file)
        /// it's more efficient to send the new file in its entirety and overwrite the destination
        /// </summary>
        Overwrite = 2
    }

}
