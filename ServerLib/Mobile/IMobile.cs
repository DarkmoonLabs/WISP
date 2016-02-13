using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    /// <summary>
    /// A game object that has a position, size and can move
    /// </summary>
    interface IMobile
    {
        List<MobileState> StateHistory { get; set; }   
    }
}
