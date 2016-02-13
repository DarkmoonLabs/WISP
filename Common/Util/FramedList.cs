using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared
{
    /// <summary>
    /// Custom list that stores objects serially in the order they were added.  Keeps "new" items in a buffer until the NewItems are read once, at which point
    /// the NewItems buffer is cleared.  Thread-Safe.
    /// </summary>
    public class FramedList<T> : IList<T>
    {
        public FramedList()
        {
            m_AllItems = new List<T>();
            m_NewItems = new List<T>();
            m_NewItemSyncRoot = new object();
        }

        private object m_NewItemSyncRoot = null;
        private List<T> m_NewItems = null;
        private List<T> m_AllItems = null;
        
        public int IndexOf(T item)
        {
            return m_AllItems.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            m_AllItems.Insert(index, item);
            lock (m_NewItemSyncRoot)
            {
                m_NewItems.Add(item);
            }
        }

        public void RemoveAt(int index)
        {
            T item = m_AllItems[index];
            m_AllItems.RemoveAt(index);
            lock (m_NewItemSyncRoot)
            {
                m_NewItems.Remove(item);
            }
        }

        public T this[int index]
        {
            get
            {
                return m_AllItems[index];
            }
            set
            {
                m_AllItems[index] = value;
            }
        }

        public void Add(T item)
        {
            lock (m_NewItemSyncRoot)
            {
                m_NewItems.Add(item);
            }
            m_AllItems.Add(item);
        }

        public void Clear()
        {
            lock (m_NewItemSyncRoot)
            {
                m_NewItems.Clear();
            }
            m_AllItems.Clear();
        }

        public bool Contains(T item)
        {
            return m_AllItems.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            m_AllItems.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return m_AllItems.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(T item)
        {
            lock (m_NewItemSyncRoot)
            {
                m_NewItems.Remove(item);
            }
            return m_AllItems.Remove(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return m_AllItems.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return m_AllItems.GetEnumerator();
        }

        public List<T> GetNewItems(bool clearNewFlag)
        {
            List<T> items = null;
            lock (m_NewItemSyncRoot)
            {
                items = new List<T>();
                items.AddRange(m_NewItems);
                if (clearNewFlag)
                {
                    m_NewItems.Clear();
                }
            }
            return items;
        }
    }
}
