using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Shared
{
    /// <summary>
    /// Queue that handles one asynchronous task at a time.  
    /// Multiple queues of this type will run their tasks in parallel using the .NET PFX I/O completion port queueing engine. All public methods/accessors are thread safe.
    /// If you want to be notified when a particular task completes, add a continuation task via YourTask.ContinueWith() before submitting it to the queue. For quickest notifications use the 
    /// TaskContinuationOptions.ExecuteSynchronously parameter for your continuation tasks.
    /// </summary>
    public class PFXSingleTaskQueue
    {
        /// <summary>
        /// Fires when the queue becomes depleted
        /// </summary>
        public event Action<PFXSingleTaskQueue> QueueEmpty;

        /// <summary>
        /// Stores tasks to be executed by the I/O completion port queue
        /// </summary>
        private Queue<Task> m_TaskQueue = new Queue<Task>();               
        
        /// <summary>
        /// Private backer
        /// </summary>
        private static PFXSingleTaskQueue m_GlobalQ;
         
        /// <summary>
        /// A global task queue object.
        /// </summary>
        public static PFXSingleTaskQueue Global
        {
            get
            {
                if (m_GlobalQ == null)
                {
                    m_GlobalQ = new PFXSingleTaskQueue();
                }
                return m_GlobalQ;
            }
            set { m_GlobalQ = value; }
        }

        /// <summary>
        /// Private backer
        /// </summary>
        private bool m_TaskRunning = false;

        /// <summary>
        /// Are there currently tasks running or is the queue empty.
        /// </summary>
        public bool IsRunning
        {
            get
            {
                lock (m_TaskQueue)
                {
                    return m_TaskRunning;
                }
            }
        }
        
        /// <summary>
        /// Enqueues a task to the I/O completion port task queue
        /// </summary>
        /// <param name="t"></param>
        public void AddTask(Task t)
        {            
            // Hook up task complete callback via TaskContinuation mechanism.
            t.ContinueWith(parent =>
            {
                OnTaskComplete(parent);
            }, TaskContinuationOptions.ExecuteSynchronously);

            // See if we have a task running already or if we can kick new task off immediately
            lock (m_TaskQueue)
            {
                if (m_TaskRunning)
                {
#if DEBUG
                    if (t.AsyncState != null)
                    {
                        Log1.Logger("|||PFX|||").Debug("Queueing PFX task: " + t.AsyncState.ToString());
                    }
#endif
                    // already have a task running. enqueue
                    m_TaskQueue.Enqueue(t);
                }
                else
                {
#if DEBUG
                    if (t.AsyncState != null)
                    {
                        Log1.Logger("|||PFX|||").Debug("Immediately Starting PFX task: " + t.AsyncState.ToString());
                    }
#endif
                    m_TaskRunning = true;
                    t.Start();
                }
            }
        }

        /// <summary>
        /// If the queue is currently running, the event handler is attached and called when the queue is empty.
        /// </summary>
        /// <param name="callback">the callback to call when the queue is empty</param>
        /// <returns>true if the callback was attached and the queue is currently running.  false, otherwise.</returns>
        public bool NotifyEmptyIfRunning(Action<PFXSingleTaskQueue> callback)
        {
            lock (m_TaskQueue)
            {
                if (m_TaskRunning)
                {
                    QueueEmpty += callback;
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Local callback that fires when a task is completed.  Dequeus the next task, if any, and starts it.
        /// </summary>
        /// <param name="completedTask"></param>
        private void OnTaskComplete(Task completedTask)
        {
#if DEBUG
            if (completedTask.AsyncState != null)
            {
                Log1.Logger("|||PFX|||").Debug("Completed PFX task: " + completedTask.AsyncState.ToString());
            }
#endif
            Task next = null;
            lock (m_TaskQueue)
            {
                if (m_TaskQueue.Count > 0)
                {
                    next = m_TaskQueue.Dequeue();
                }
                else
                {
                    //Release the lock
                    m_TaskRunning = false;
                    if (QueueEmpty != null)
                    {
                        QueueEmpty(this);
                    }
                }
            }

            if (next != null)
            {
#if DEBUG
                if (completedTask.AsyncState != null)
                {
                    Log1.Logger("|||PFX|||").Debug("Starting PFX task: " + next.AsyncState.ToString());
                }
#endif
                next.RunSynchronously();                
            }
        }
    }
}
