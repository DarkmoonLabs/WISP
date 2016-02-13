using System;
using System.Collections.Generic;

namespace Shared
{
    /// <summary>
    /// A numeric statistic about a game object.
    /// </summary>
    public class Stat
    {
        /// <summary>
        /// The stat bag to which this stat is attached
        /// </summary>
        public StatBag Owner { get; set; }

        /// <summary>
        /// The ID of the stat
        /// </summary>
        public int StatID { get; set; }

        /// <summary>
        /// The group to which this stat belongs
        /// </summary>
        public string Group { get; set; }

        /// <summary>
        /// Short line item text
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Lon ger, descriptive, tooltip type text
        /// </summary>
        public string Description { get; set; }

        private float m_CurrentValue = 0;

        /// <summary>
        /// The current numerical value of the stat
        /// </summary>
        [System.Xml.Serialization.XmlIgnore]
        public float CurrentValue
        {
            get
            {
                return m_CurrentValue;
            }
            private set
            {
                if (value < MinValue)
                {
                    value = MinValue;
                }
                else if (value > MaxValue)
                {
                    value = MaxValue;
                }

                m_CurrentValue = value;
            }
        }

        /// <summary>
        /// The maximum value that the stat can be
        /// </summary>
        public float MaxValue { get; set; }

        /// <summary>
        /// The minimum value that the stat can be
        /// </summary>
        public float MinValue { get; set; }

        public Stat()
        {
        }

        public Stat Copy()
        {
            Stat s = new Stat();
            s.StatID = this.StatID;
            s.DisplayName = this.DisplayName;
            s.Description = this.Description;
            s.Group = this.Group;
            s.CurrentValue = this.CurrentValue;
            s.MinValue = this.MinValue;
            s.MaxValue = this.MaxValue;
            return s;
        }

        public Stat(int statID, string displayName, string desc, string group, float curVal, float minVal, float maxVal)
        {
            StatID = statID;
            DisplayName = displayName;
            MinValue = minVal;
            MaxValue = maxVal;
            CurrentValue = curVal;
            Group = group;
            Description = desc;
        }

        #region DECREASE BY PERCENT
        private float DecreaseValueByPercent(float val, List<string> msgs, float min, float max)
        {
            val = Math.Abs(val);
            float extraAmount = (float)Math.Ceiling(CurrentValue * val) * -1;
            if (extraAmount != 0)
            {
                CurrentValue += extraAmount;

                if (min != -1 && CurrentValue < min)
                {
                    CurrentValue = min;
                }

                if (max != -1 && CurrentValue > max)
                {
                    CurrentValue = max;
                }

            }
            return CurrentValue;
        }

        public float DecreaseValueByPercent(float val, List<string> msgs)
        {
            return DecreaseValueByPercent(val, msgs, -1, -1);
        }


        #endregion

        #region INCREASE BY PERCENT
        private float IncreaseValueByPercent(float val, List<string> msgs, float min, float max)
        {
            val = Math.Abs(val);
            float extraAmount = (float)Math.Ceiling(CurrentValue * val);

            if (extraAmount != 0)
            {
                CurrentValue += extraAmount;

                if (min != -1 && CurrentValue < min)
                {
                    CurrentValue = min;
                }

                if (max != -1 && CurrentValue > max)
                {
                    CurrentValue = max;
                }

            }

            return CurrentValue;
        }


        public float IncreaseValueByPercent(float val, List<string> msgs)
        {
            return IncreaseValueByPercent(val, msgs, -1, -1);
        }

        #endregion

        #region SET TO VALUE
        private float SetValue(float val, List<string> msgs, float min, float max)
        {
            float extraAmount = val - CurrentValue;
            if (extraAmount != 0)
            {
                CurrentValue = val;

                if (min != -1 && CurrentValue < min)
                {
                    CurrentValue = min;
                }

                if (max != -1 && CurrentValue > max)
                {
                    CurrentValue = max;
                }

            }
            return CurrentValue;
        }


        public float SetValue(float val, List<string> msgs)
        {
            return SetValue(val, msgs, -1, -1);
        }

        public float ForceValue(float val)
        {
            CurrentValue = val;
            return CurrentValue;
        }


        #endregion

        #region DECREASE BY FLAT AMOUNT
        private float DecreaseValue(float val, List<string> msgs, float min, float max)
        {
            if (val != 0)
            {
                CurrentValue += (float)val * -1f; // 'amount' is a negative number after it comes back from BeforeEventOccured, that's why we "+=" it instead of "-=".

                if (min != -1 && CurrentValue < min)
                {
                    CurrentValue = min;
                }

                if (max != -1 && CurrentValue > max)
                {
                    CurrentValue = max;
                }

            }
            return CurrentValue;
        }

        public float DecreaseValue(float val, List<string> msgs)
        {
            return DecreaseValue(val, msgs, -1, -1);
        }

        #endregion

        #region INCREASE BY FLAT AMOUNT
        private float IncreaseValue(float val, List<string> msgs, float min, float max)
        {
            CurrentValue += val;

            if (min != -1 && CurrentValue < min)
            {
                CurrentValue = min;
            }

            if (max != -1 && CurrentValue > max)
            {
                CurrentValue = max;
            }

            return CurrentValue;
        }

        public float IncreaseValue(float val, List<string> msgs)
        {
            return IncreaseValue(val, msgs, -1, -1);
        }

        #endregion
    }
}