using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace Shared
{
    public interface IPersistable
    {
        bool PersistToDisk(Dictionary<string, object> data, string fullSaveToPath);
        Dictionary<string, object> LoadFromDisk(string fullLoadFromPath);
    }
}
