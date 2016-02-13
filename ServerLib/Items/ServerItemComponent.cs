using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared
{
    public abstract class ServerItemComponent : Component, IPropertyBagOwner, IStatBagOwner
    {
        public virtual PropertyBag AddedProperties
        {
            get
            {
                return null;
            }
        }

        public virtual StatBag AddedStats
        {
            get
            {
                return null;
            }
        }

        public virtual void OnPropertyUpdated(Guid bag, Property p)
        {
        }

        public void OnPropertyAdded(Guid bag, Property p)
        {
        }

        public void OnPropertyRemoved(Guid bag, Property p)
        {
        }

        public void OnStatUpdated(Guid bag, Stat p)
        {
        }

        public void OnStatAdded(Guid bag, Stat p)
        {
        }

        public void OnStatRemoved(Guid bag, Stat p)
        {
        }

    }
}
