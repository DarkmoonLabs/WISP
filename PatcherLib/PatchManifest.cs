using System;
using System.Collections.Generic;
using System.Text;

namespace PatcherLib
{
    public class PatchManifest
    {
        public PatchManifest()
        {
            Differences = new List<Diff>();
        }

        public string FromVersionNumber { get; set; }
        public string ToVersionNumber { get; set; }
        public string PatchNotes { get; set; }
        public List<Diff> Differences { get; set; }
    }
}
