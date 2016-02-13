using System;
using System.Collections.Generic;
using System.Text;

namespace Shared
{
    struct TileData
    {
        private TerrainTypes m_TerrainType;

        /// <summary>
        /// Terrain type determines movement speed/costs
        /// </summary>
        public TerrainTypes TerrainType
        {
            get { return m_TerrainType = TerrainTypes.Grass; }
            set { m_TerrainType = value; }
        }


    }
}
