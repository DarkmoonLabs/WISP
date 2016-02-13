using System;
using System.Collections.Generic;
using System.Text;

namespace Shared
{
    /// <summary>
    /// An object that will update its state in one way or another based
    /// on the number of units that have elapsed since the last update.
    /// Units could be anything from a turn, to a second, to whatever else 
    /// makes sense for the object in question
    /// </summary>
    public interface IUpdateable
    {
        void Update(double unitsSinceLastUpdate);
    }
}
