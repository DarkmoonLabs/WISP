using System;
using System.Collections.Generic;
using System.Text;

namespace Shared
{
    /// <summary>
    /// Defines a play area within which a game takes place.
    /// Play areas could be game boards, a map, etc.
    /// </summary>
    public class PlayArea
    {
        /// <summary>
        /// Private default constructor - this is a singleton object.
        /// </summary>
        protected PlayArea() { }

        // Zones, can contain other zones
        // Main zone might contain sub-zones. I.e. game table might contain tiles for movement, draw pile, etc
        // zones can contain zero or more game objects

        private List<Region> m_Regions = new List<Region>();
        /// <summary>
        /// The play area zones contained within the play area
        /// </summary>
        public List<Region> Regions
        {
            get { return m_Regions; }
            set { m_Regions = value; }
        }

        private static PlayArea m_Instance = null;
        /// <summary>
        /// The play area instance - only ever one play area object per game
        /// </summary>
        public static PlayArea Instance
        {
            get 
            {
                if (m_Instance != null)
                {
                    return m_Instance;
                }

                m_Instance = new PlayArea();
                return m_Instance;
            }            
        }
	
    }
}
