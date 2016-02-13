using System;
using System.Collections.Generic;
using System.Text;


namespace Shared
{
    /// <summary>
    /// Point Of Interest on the map.  Could be anything from a resource node to a stronghold to 
    /// a spawn point of some kind.  Many POIs may also be owned by some game object (most notably players).
    /// </summary>
    public abstract class POI : GenericGameObject
    {
        public POI() : base()
        {
        }

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

        private long m_Owner = -1;

        /// <summary>
        /// The object that owns this POI.  A value of -1 indicates that the "system" owns the POI
        /// </summary>
        public long Owner
        {
            get { return m_Owner; }
            set { m_Owner = value; }
        }

        private SVector3 m_Location = SVector3.Zero;
        
        /// <summary>
        /// The location on the map for this POI
        /// </summary>
        public SVector3 Location
        {
            get { return m_Location; }
            set { m_Location = value; }
        }

        private Region m_EffectiveRegion;
        /// <summary>
        /// Since a Location is simply an infinitely small point in space, any POI must also specify a region
        /// that defines its area of influence.
        /// </summary>
        public Region EffectiveRegion
        {
            get { return m_EffectiveRegion; }
            set { m_EffectiveRegion = value; }
        }

	
    }
}
