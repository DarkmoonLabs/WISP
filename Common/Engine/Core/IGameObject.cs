using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Shared;

namespace Shared
{
    /// <summary>
    /// Basic information about a game object.
    /// </summary>
    public interface IGameObject : IMessagable, ISerializableWispObject
    {
        // Basic data
        Guid Owner { get; set; }
        string ObjectName { get; set; }
        Guid UID { get; set; }
        Guid Context { get; set; }

        DateTime CreatedOn { get; set; }
        string ItemTemplate { get; set; }
        GOT GameObjectType { get; set; }
        
        // Properties and stats
        PropertyBag Properties { get; set; }
        StatBag Stats { get; set; }
        
        // Game objects can have scripts and effects attached to them
        ObjectScriptManager Scripts { get; set; }

        EffectManager Effects { get; set; }

        /// <summary>
        /// Current position
        /// </summary>
        MobileState PhysicalState { get; set; }

        object Tag { get; set; }

        /// <summary>
        /// How many of this item there are in the stack
        /// </summary>
        int StackCount { get; set; } 

    }
}
