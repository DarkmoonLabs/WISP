using System;
using System.Collections.Generic;
using System.Text;

namespace Shared
{
    /// <summary>
    /// Tracks information for one tile.
    /// </summary>
    public class MapTile : Region, IPathNode<IGameObject>
    {
        public MapTile() : base()
        {
        }

        private GameObjectContainer m_ContainedObjects = new GameObjectContainer();
        /// <summary>
        /// The game objects currently on this tile
        /// </summary>
        public GameObjectContainer Contained
        {
            get { return m_ContainedObjects; }
            set { m_ContainedObjects = value; }
        }

        /// <summary>
        /// Called by pathfinding algorithm to determine if tile is traversable.
        /// </summary>
        /// <param name="inContext"></param>
        /// <returns></returns>
        public virtual bool IsWalkable(IGameObject inContext)
        {
            return true;
        }

        private SVector3 m_WorldLocation;
        public SVector3 WorldLocation
        {
            get { return m_WorldLocation; }
            set { m_WorldLocation = value; }
        }

        public int Row = 0;
        public int Column = 0;

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
    }
}
