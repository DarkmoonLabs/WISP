using System;
using System.Collections.Generic;
using System.Text;
using Shared;
using System.Linq;
using System.Threading;

namespace Shared
{
    /// <summary>
    /// Here is where we store all the GameObject instances
    /// </summary>
    public class GameObjectManager
    {
        private Guid m_Context;
        public Guid Context
        {
            get { return m_Context; }
        }
        
        public GameObjectManager(Guid context)
        {
            m_Context = context;
        }

        public int ObjectCount
        {
            get
            {
                return m_GameObjects.Count;
            }
        }
     
        public ICollection<IGameObject> AllObjects
        {
            get
            {
                return m_GameObjects.Values;
            }
        }
        
        // All objects
        private ThreadSafeDictionary<Guid, IGameObject> m_GameObjects = new ThreadSafeDictionary<Guid, IGameObject>(new GUIDEqualityComparer());

        public virtual void RegisterGameObject(IGameObject actor, Guid context)
        {
            m_GameObjects.MergeSafe(actor.UID, actor);
        }

        public virtual void RemoveGameObject(IGameObject actor)
        {
            m_GameObjects.RemoveSafe(actor.UID);
        }

        public IGameObject GetGameObjectFromId(Guid id)
        {
            IGameObject go = null;
            m_GameObjects.TryGetValue(id, out go);
            return go;
        }

    }
}
