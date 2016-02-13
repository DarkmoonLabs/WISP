using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared
{
    /// <summary>
    /// Custom equality comparer. Required for Generic dictionaries on iOS so as to circumvent the JIT restrictions on that platform.
    /// </summary>
    public class GUIDEqualityComparer : IEqualityComparer<Guid>
    {
        public GUIDEqualityComparer()
        {
        }

        public bool Equals(Guid x, Guid y)
        {
            return x.Equals(y);
        }

        public int GetHashCode(Guid obj)
        {
            return obj.GetHashCode();
        }
    }
}
