using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Shared;

namespace Shared
{
    /// <summary>
    /// the shared data across all effects of a particular type.
    /// </summary>
    public class EffectInfo
    {
        public EffectInfo()
        {
            Duration = -1;
            DurationKind = EffectDurationType.Time;
            Group = "";
            DisplayName = "";
            Description = "";
            EventsToListenTo = new GameEventType[0];
            TickLength = -1;
        }

        /// <summary>
        /// The kind of effect that it is
        /// </summary>
        public uint EffectKind { get; set; }

        
        /// <summary>
        /// How long does the effect last?  If its a time buff this number is TimeSpan.FromTick(Duration), if it's turns, then it's the number
        /// of turns that the player must take before it wears off
        /// </summary>
        public long Duration { get; set; }

        /// <summary>
        /// Does this buff last for time or turns
        /// </summary>
        public EffectDurationType DurationKind { get; set; }

        public string Group { get; set; }
        /// <summary>
        /// Short line item text
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Lon ger, descriptive, tooltip type text
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The events that this effect wants to be notified for
        /// </summary>
        public GameEventType[] EventsToListenTo { get; set; }

        /// <summary>
        /// How often the "Tick" event occurs. how this is interpreted depends on the DurationKind set in the effect
        /// </summary>
        public long TickLength { get; set; }

    }

}
