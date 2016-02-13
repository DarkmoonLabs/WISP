using System;
using System.Collections.Generic;
using System.Text;

namespace Shared
{
    /// <summary>
    /// A game object that can contain other game objects
    /// </summary>
    public class GameObjectContainer
    {
        /// <summary>
        /// Contains a reference to every game object contained in this container.
        /// </summary>
        private Dictionary<Guid, IGameObject> m_ObjectsById = new Dictionary<Guid, IGameObject>(new GUIDEqualityComparer());

        /// <summary>
        /// Gets a reference to a game object within this container, given that object's ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public IGameObject GetObject(Guid id)
        {
            if (m_ObjectsById.ContainsKey(id))
            {
                return m_ObjectsById[id];
            }

            return null;
        }

        public Dictionary<Guid, IGameObject>.ValueCollection.Enumerator Objects
        {
            get
            {
                return m_ObjectsById.Values.GetEnumerator();
            }
        }

        public IGameObject[] ToObjects
        {
            get
            {
                IGameObject[] objs = new IGameObject[m_ObjectsById.Count];
                m_ObjectsById.Values.CopyTo(objs, 0);
                return objs;
            }
        }

        public List<IGameObject> GetObjectsOfType(GOT type)
        {
            List<IGameObject> rslt = new List<IGameObject>();
            foreach (IGameObject go in m_ObjectsById.Values)
            {
                if ((go.GameObjectType & type) != 0)
                {
                    rslt.Add(go);
                }
            }

            return rslt;
        }

        public int AddObject(IGameObject go)
        {
            if (!m_ObjectsById.ContainsKey(go.UID))
            {
                m_ObjectsById.Add(go.UID, go);
            }

            return m_ObjectsById.Count;
        }

        public int RemoveObject(Guid id)
        {
            m_ObjectsById.Remove(id);
            return m_ObjectsById.Count;
        }

        /// <summary>
        /// The number of objects being stored within
        /// </summary>
        public int Count
        {
            get { return m_ObjectsById.Count; }
        }
    }
}
