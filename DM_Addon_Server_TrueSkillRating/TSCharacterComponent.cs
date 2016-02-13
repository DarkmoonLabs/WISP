using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Moserware.Skills.TrueSkill;
using Moserware.Skills;

namespace Shared
{
    public class TSCharacterComponent : ServerCharacterComponent
    {
        private static uint m_TypeHash = 0;
        public override uint TypeHash
        {
            get
            {
                if (m_TypeHash == 0)
                {
                    m_TypeHash = Factory.GetTypeHash(this.GetType());
                }

                return m_TypeHash;
            }
        }

        public TSCharacterComponent()
        {
            ComponentName = "TrueSkill Character Rating Component";
            m_Properties = new PropertyBag("True Skill Added Properties");
            
            GameInfo gameInfo = GameInfo.DefaultGameInfo;
            int gamesPlayed = 0;

            m_Properties.SetProperty("TS Games Played", (int)TSPropertyID.GamesPlayed, gamesPlayed);
            m_Properties.SetProperty("Initial Mean", (int)TSPropertyID.RatingMean, gameInfo.InitialMean);
            m_Properties.SetProperty("Initial Standard Deviation", (int)TSPropertyID.RatingStandardDeviation, gameInfo.InitialStandardDeviation);
        }

        private PropertyBag m_Properties;
        public override PropertyBag AddedProperties
        {
            get
            {
                return m_Properties;
            }
        }

        /// <summary>
        /// The total number of games that this character has played in.
        /// </summary>
        public int GamesPlayed
        {
            get
            {
                return m_Properties.GetIntProperty((int)TSPropertyID.GamesPlayed).GetValueOrDefault();
            }
            set
            {
                m_Properties.SetProperty((int)TSPropertyID.GamesPlayed, value);
            }
        }

        /// <summary>
        /// Character rating mean
        /// </summary>
        public double RatingMean
        {
            get
            {
                return m_Properties.GetDoubleProperty((int)TSPropertyID.RatingMean).GetValueOrDefault();
            }
            set
            {
                m_Properties.SetProperty((int)TSPropertyID.RatingMean, value);
            }
        }

        /// <summary>
        /// Character rating standard deviation
        /// </summary>
        public double RatingStandardDeviation
        {
            get
            {
                return m_Properties.GetDoubleProperty((int)TSPropertyID.RatingStandardDeviation).GetValueOrDefault();
            }
            set
            {
                m_Properties.SetProperty((int)TSPropertyID.RatingStandardDeviation, value);
            }
        }

        public override void OnPropertyUpdated(Guid bag, Property p)
        {
            switch (p.PropertyId)
            {
                case (int)TSPropertyID.GamesPlayed:
                case (int)TSPropertyID.RatingMean:
                case (int) TSPropertyID.RatingStandardDeviation:
                    m_Properties.AddProperty(p);
                    break;
            }

            base.OnPropertyUpdated(bag, p);
        }

        public override void Serialize(ref byte[] buffer, Pointer p)
        {
            BitPacker.AddInt(ref buffer, p, GamesPlayed);
            BitPacker.AddDouble(ref buffer, p, RatingMean);
            BitPacker.AddDouble(ref buffer, p, RatingStandardDeviation);

            base.Serialize(ref buffer, p);
        }

        public override void Deserialize(byte[] data, Pointer p)
        {
            GamesPlayed = BitPacker.GetInt(data, p);
            RatingMean = BitPacker.GetDouble(data, p);
            RatingStandardDeviation = BitPacker.GetDouble(data, p);

            base.Deserialize(data, p);
        }
     
    }
}
