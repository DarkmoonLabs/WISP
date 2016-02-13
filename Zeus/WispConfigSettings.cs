using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared
{
    /// <summary>
    /// Wisp object that lists all of the config settings available on the server
    /// </summary>
    public class WispConfigSettings : ISerializableWispObject
    {
        public Dictionary<string, string> Configs = new Dictionary<string, string>();
        private static uint m_TypeHash = 0;
        public virtual uint TypeHash
        {
            get
            {
                if (m_TypeHash == 0)
                {
                    m_TypeHash = Factory.GetTypeHash(this.GetType());
                }

                return m_TypeHash;
            }
        }

        public void Serialize(ref byte[] buffer, Pointer p)
        {
            BitPacker.AddInt(ref buffer, p, Configs.Count);
            Dictionary<string, string>.Enumerator enu = Configs.GetEnumerator();
            while (enu.MoveNext())
            {
                BitPacker.AddString(ref buffer, p, enu.Current.Key);
                BitPacker.AddString(ref buffer, p, enu.Current.Value);
            }
        }

        public void Deserialize(byte[] data, Pointer p)
        {
            int count = BitPacker.GetInt(data, p);
            for (int i = 0; i < count; i++)
            { 
                string key = BitPacker.GetString(data, p);
                string value = BitPacker.GetString(data, p);
                if (!Configs.ContainsKey(key))
                {
                    Configs.Add(key, value);
                }
            }
        }
    }
}
