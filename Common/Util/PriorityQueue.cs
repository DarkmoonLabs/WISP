using System;
using System.Text;
using System.Collections;

namespace Shared
{
    public struct HeapEntry<TItem>
    {
        private TItem item;
        private long priority;

        public HeapEntry(TItem item, long priority)
        {
            this.item = item;
            this.priority = priority;
        }

        public TItem Item
        {
            get
            {
                return item;
            }
        }

        public long Priority
        {
            get
            {
                return priority;
            }
        }
    }

    public class PriorityQueue<Titem> : ICollection
    {
        private long m_Count;
        private long m_Capacity;
        private long m_Version;
        private HeapEntry<Titem>[] m_Heap;

        public PriorityQueue()
        {
            m_Capacity = 15; // 15 is equal to 4 complete levels
            m_Heap = new HeapEntry<Titem>[m_Capacity];
        }

        public PriorityQueue(long capacity)
        {
            m_Capacity = capacity;
            m_Heap = new HeapEntry<Titem>[capacity];
        }

        public Titem HighestPriorityItem
        {
            get
            {
                if (m_Count == 0)
                {
                    return default(Titem);
                }

                return m_Heap[0].Item;
            }
        }

        public Titem Dequeue()
        {
            if (m_Count == 0)
            {
                throw new InvalidOperationException();
            }

            Titem result = m_Heap[0].Item;
            m_Count--;
            trickleDown(0, m_Heap[m_Count]);
            m_Version++;
            return result;
        }

        public void Enqueue(Titem item, long priority)
        {
            if (m_Count == m_Capacity)
            {
                growHeap();
            }
            m_Count++;
            bubbleUp(m_Count - 1, new HeapEntry<Titem>(item, priority));
            m_Version++;
        }

        private void bubbleUp(long index, HeapEntry<Titem> he)
        {
            long parent = getParent(index);
            // note: (index > 0) means there is a parent
            while ((index > 0) && (m_Heap[parent].Priority < he.Priority))
            {
                m_Heap[index] = m_Heap[parent];
                index = parent;
                parent = getParent(index);
            }
            m_Heap[index] = he;
        }

        private long getLeftChild(long index)
        {
            return (index * 2) + 1;
        }

        private long getParent(long index)
        {
            return (index - 1) / 2;
        }

        private void growHeap()
        {
            m_Capacity = (m_Capacity * 2) + 1;
            HeapEntry<Titem>[] newHeap = new HeapEntry<Titem>[m_Capacity];
            System.Array.Copy(m_Heap, 0, newHeap, 0, (int)m_Count);
            m_Heap = newHeap;
        }

        private void trickleDown(long index, HeapEntry<Titem> he)
        {
            long child = getLeftChild(index);
            while (child < m_Count)
            {
                if (((child + 1) < m_Count) &&
                    (m_Heap[child].Priority < m_Heap[child + 1].Priority))
                {
                    child++;
                }
                m_Heap[index] = m_Heap[child];
                index = child;
                child = getLeftChild(index);
            }
            bubbleUp(index, he);
        }

        #region IEnumerable implementation
        public IEnumerator GetEnumerator()
        {
            return new PriorityQueueEnumerator<Titem>(this);
        }
        #endregion

        #region ICollection implementation
        public int Count
        {
            get { return (int)m_Count; }
        }


        public void CopyTo(Array array, int index)
        {
            System.Array.Copy(m_Heap, 0, array, index, (int)m_Count);
        }

        public object SyncRoot
        {
            get { return this; }
        }

        public bool IsSynchronized
        {
            get { return false; }
        }
        #endregion

        #region Priority Queue enumerator
        private class PriorityQueueEnumerator<TItem> : IEnumerator
        {
            private long index;
            private PriorityQueue<Titem> pq;
            private long version;

            public PriorityQueueEnumerator(PriorityQueue<Titem> pq)
            {
                this.pq = pq;
                Reset();
            }

            private void checkVersion()
            {
                if (version != pq.m_Version)
                    throw new InvalidOperationException();
            }

            #region IEnumerator Members

            public void Reset()
            {
                index = -1;
                version = pq.m_Version;
            }

            public object Current
            {
                get
                {
                    checkVersion();
                    return pq.m_Heap[index].Item;
                }
            }

            public bool MoveNext()
            {
                checkVersion();
                if (index + 1 == pq.m_Count)
                    return false;
                index++;
                return true;
            }

            #endregion
        }
        #endregion

    }
}
