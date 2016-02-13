using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Shared
{
    /// <summary>
    /// Multi-threaded producer/consumer queue using manual thread pool
    /// </summary>
    public class PCQueue
    {
        /// <summary>
        /// Thread synchronization object
        /// </summary>
        readonly object m_SyncRoot = new object();
        
        /// <summary>
        /// Worker threads. number of threads is immutable after instantiation
        /// </summary>
        private Thread[] m_WorkerThreads ;

        /// <summary>
        /// Work item queue
        /// </summary>
        private Queue<Action> m_WorkItemQ = new Queue<Action>();

        /// <summary>
        /// Should queue be purged when it is being shut down or should queued work items be allowed to execute
        /// </summary>
        private bool m_BailOut = false;

        /// <summary>
        /// Creates a new producer/consumer queue with the given number of consumer threads
        /// </summary>
        /// <param name="workerThreadCount">the number of threads you want to simultaneous consume work items</param>
        public PCQueue(int workerThreadCount)
        {
            m_WorkerThreads = new Thread[workerThreadCount];
            // Create and start a separate thread for each worker
            for (int i = 0; i < workerThreadCount; i++)
            {
                Thread nt = new Thread(new ThreadStart(Consume));
                nt.IsBackground = true;
                m_WorkerThreads[i] = nt;
                nt.Start();
                //(m_WorkerThreads[i] = new Thread(Consume)).Start();
            }
        }

        /// <summary>
        /// Signals the shutdown of the queue.  All previously enqueued work items will still be processed unless abandonExistingWorkItems is specified as true
        /// </summary>
        /// <param name="waitForWorkers">block method until all consumer threads have returned?</param>
        /// <param name="abandonExistingWorkItems">should we abandon currently enqueued work items before ending the consumer threads?</param>
        /// <param name="cancelInProgress">if a work item is currently being processed, we can attempt to shut down that consumer thread by throwing an abort exception at it</param>
        public void Shutdown(bool waitForWorkers, bool abandonExistingWorkItems, bool cancelInProgress)
        {
            m_BailOut = abandonExistingWorkItems;
            
            // Enqueue one null item per worker to signal an exit.
            foreach (Thread worker in m_WorkerThreads)
            {
                EnqueueItem(null);
                if (cancelInProgress)
                {
                    worker.Abort();
                }
            }

            // Wait for workers to finish
            if (waitForWorkers)
            {
                foreach (Thread worker in m_WorkerThreads)
                {
                    worker.Join();
                }
            }
        }

        /// <summary>
        /// Enter a work item into the queue
        /// </summary>
        /// <param name="item"></param>
        public void EnqueueItem(Action item)
        {
            lock (m_SyncRoot)
            {
                m_WorkItemQ.Enqueue(item);           // We must pulse because we're
                Monitor.Pulse(m_SyncRoot);         // changing a blocking condition.
            }
        }

        /// <summary>
        /// Consumes the queue until it is being shut down or it receives the exit signal (a NULL work item in the queue)
        /// </summary>
        private void Consume()
        {
            while (!m_BailOut) // bailout may be true if the queue is being shut down
            {                        
                Action action;
                lock (m_SyncRoot)
                {
                    while (m_WorkItemQ.Count == 0)
                    {
                        // Acquire a lock on the queue so we don't contend for the same item with another thread in our pool
                        Monitor.Wait(m_SyncRoot);
                    }
                    action = m_WorkItemQ.Dequeue();
                }

                if (action == null) // exit signal
                {
                    return;
                }

                // Run work item
                action();                           
            }

            // We can only get here if the queue is being shut down forcibly, so we purge the queue.
            m_WorkItemQ.Clear();
        }

    }
}
