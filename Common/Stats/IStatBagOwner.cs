using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared
{
    /// <summary>
    /// An object which owns or is interested in StatBag business.  If you want to be notified
    /// by a StatBag object when one of its Stats changes, implement this interface and subscribe
    /// to the property changed requests
    /// </summary>
    public interface IStatBagOwner
    {
        void OnStatUpdated(Guid bag, Stat p);
        void OnStatAdded(Guid bag, Stat p);
        void OnStatRemoved(Guid bag, Stat p);
    }
}
