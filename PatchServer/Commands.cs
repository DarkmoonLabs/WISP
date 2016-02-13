using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Shared;

namespace PatchServer
{
    public class Commands
    {
        public void ReloadVersionsTxt(string executor)
        {
            Log1.Logger("Patcher").Info(executor + " is reloading Versions.txt.");
            PatchServerProcess.Instance.ReadPatchVersions();
        }

        public void ReloadPatchNotes(string executor)
        {
            Log1.Logger("Patcher").Info(executor + " is reloading PatchNotes.txt.");
            PatchServerProcess.Instance.ReadPatchNotes();
        }
    }
}
