using System;
using System.Collections.Generic;
using System.Text;

namespace Shared
{
    /// <summary>
    /// One map chunk - contains information about a given section of map
    /// </summary>
    public class MapChunk
    {
        private MapChunk() { }

        /// <summary>
        /// Creates a new map chunk
        /// </summary>
        /// <param name="x">address within the Chunk grid</param>
        /// <param name="y">address within the Chunk grid</param>
        /// <param name="z">address within the Chunk grid</param>
        /// <param name="objectsToIndex">determines which GOTs get indexed.  Anytime you want to get a reference to "All of object type X" as in GetAllAvatarsInRange that object type should be indexed</param>
        public MapChunk(int chunkX, int chunkY, int chunkZ, GameMap owner)
        {
            m_Owner = owner;
            m_X = chunkX;
            m_Y = chunkY;
            m_Z = chunkZ;
        }

        private GameMap m_Owner;
        /// <summary>
        /// The map that owns this chunk
        /// </summary>
        public GameMap Owner
        {
            get { return m_Owner; }
            set { m_Owner = value; }
        }
	
        private int m_X;
        /// <summary>
        /// The X coordinate of the chunk.  Part of its "address" in the chunk grid
        /// </summary>
        public int X
        {
            get { return m_X; }
            set { m_X = value; }
        }

        private int m_Y;
        /// <summary>
        /// The Y coordinate of the chunk.  Part of its "address" in the chunk grid
        /// </summary>
        public int Y
        {
            get { return m_Y; }
            set { m_Y = value; }
        }

        private int m_Z;
        /// <summary>
        /// The Z coordinate of the chunk.  Part of its "address" in the chunk grid
        /// </summary>
        public int Z
        {
            get { return m_Z; }
            set { m_Z = value; }
        }

        private GameObjectContainer m_Inventory = new GameObjectContainer();
        /// <summary>
        /// Tracks all objects within this chunk
        /// </summary>
        public GameObjectContainer Inventory
        {
            get { return m_Inventory = new GameObjectContainer(); }
        }
	

    }
}
