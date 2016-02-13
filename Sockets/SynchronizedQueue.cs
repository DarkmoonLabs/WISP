using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Threading;
using Shared;

namespace Shared
{
    /// <summary>
    /// A standard FIFO Systems.Collection.Generic.Queue with data synchronization added to make it thread-safe
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SynchronizedQueue<T>
    {
        readonly object listLock = new object();
        Queue<T> queue = new Queue<T>();

        public IEnumerable<T> GetAllItems()
        {
            lock (listLock)
            {
                T[] items = queue.ToArray();
                queue.Clear();
                return items;
            }
        }

        /// <summary>
        /// Add an object to the queue
        /// </summary>
        /// <param name="o"></param>
        public void Add(T o)
        {
            lock (listLock)
            {
                queue.Enqueue(o);

                // We always need to pulse, even if the queue wasn't
                // empty before. Otherwise, if we add several items
                // in quick succession, we may only pulse once, waking
                // a single thread up, even if there are multiple threads
                // waiting for items.            
                Monitor.Pulse(listLock);
            }
        }

        /// <summary>
        /// Grab an object from the queue
        /// </summary>
        /// <returns></returns>
        public T Get()
        {
            lock (listLock)
            {
                // If the queue is empty, wait for an item to be added
                // Note that this is a while loop, as we may be pulsed
                // but not wake up before another thread has come in and
                // consumed the newly added object. In that case, we'll
                // have to wait for another pulse.
                while (queue.Count == 0)
                {
                    // This releases listLock, only reacquiring it
                    // after being woken up by a call to Pulse
                    Monitor.Wait(listLock);
                }

                return queue.Dequeue();
            }
        }
    }

}
