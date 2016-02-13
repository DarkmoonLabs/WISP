using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared
{
    /// <summary>
    /// An object which owns or is interested in PropertyBag business.  If you want to be notified
    /// by a PropertyBag object when one of its properties changes, implement this interface and subscribe
    /// to the property changed requests
    /// </summary>
    public interface IPropertyBagOwner
    {
        void OnPropertyUpdated(Guid bag, Property p);
        void OnPropertyAdded(Guid bag, Property p);
        void OnPropertyRemoved(Guid bag, Property p);
    }
}
