using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared
{
    /// <summary>
    /// Defines what property objects for property bags look like
    /// </summary>
    public interface IPropertyBagProperty<T>
    {
        T Value { get; set; }
    }
}
