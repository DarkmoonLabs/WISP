using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Shared;

namespace Shared
{
    public class WispCharacterDetail : CharacterInfo
    {
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
            base.Serialize(ref buffer, p, false);
        }

        public void Deserialize(byte[] data, Pointer p)
        {
            base.Deserialize(data, p, false);
        }

        public WispCharacterDetail(int id) : base(id)
        {
            
        }

    }
}
