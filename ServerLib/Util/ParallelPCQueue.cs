using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;


namespace Shared
{
    public class ParallelPCQueue : IDisposable
    {
        private class WorkItem
        {
            public readonly TaskCompletionSource<object> TaskSource;
            public readonly Action Action;
            public readonly CancellationToken? CancelToken;

            public WorkItem(TaskCompletionSource<object> taskSource, Action action, CancellationToken? cancelToken)
            {
                TaskSource = taskSource;
                Action = action;
                CancelToken = cancelToken;
            }
        }

        BlockingCollection<WorkItem> m_TaskQ = new BlockingCollection<WorkItem>(new ConcurrentQueue<WorkItem>());

        public ParallelPCQueue(int workerCount)
        {
            // Create and start a separate Task for each consumer:
            for (int i = 0; i < workerCount; i++)
            {
                Task.Factory.StartNew(Consume);
            }
        }

        public void Dispose() 
        { 
            m_TaskQ.CompleteAdding();  // kill the Consume loop.
        }

        public Task EnqueueTask(Action action)
        {
            return EnqueueTask(action, null);
        }

        public Task EnqueueTask(Action action, CancellationToken? cancelToken)
        {            
            var tcs = new TaskCompletionSource<object>();
            m_TaskQ.Add(new WorkItem(tcs, action, cancelToken));
            return tcs.Task;
        }

        private void Consume()
        {
            foreach (WorkItem workItem in m_TaskQ.GetConsumingEnumerable()) // block until more work items are in the queue.  loops infinitely.
            {
                if (workItem.CancelToken.HasValue && workItem.CancelToken.Value.IsCancellationRequested)
                {
                    workItem.TaskSource.SetCanceled();
                }
                else
                {
                    try
                    {
                        workItem.Action();
                        workItem.TaskSource.SetResult(null);   // Indicate completion
                    }
                    catch (Exception ex)
                    {
                        workItem.TaskSource.SetException(ex);
                    }
                }
            }
        }

    }
}
