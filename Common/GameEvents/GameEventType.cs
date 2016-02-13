using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared
{
    public enum GameEventType
    {
        None,
        CharacterLoaded,
        ResourceAmountChanged,
        LandChanged,
        CharacterStatChanged,
        ArmyStatChanged,
        EffectDetached,
        EffectAttached,
        ObjectLoaded,
    }
}