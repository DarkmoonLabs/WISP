using System;
using System.Collections.Generic;
namespace Shared
{
    /// <summary>
    /// Basic information about a player character.
    /// </summary>
    public interface ICharacterInfo : IComponent
    {
        string CharacterName { get; set; }
        int ID { get; set; }
        DateTime LastLogin { get; set; }
        PropertyBag Properties { get; set; }
        StatBag Stats { get; set; }        
    }
}
