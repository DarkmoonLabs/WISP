using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;

namespace Shared
{    
    /// <summary>
    /// Internally used to track TCP buffers.
    /// </summary>
    public sealed class SocketAsyncEventArgsStack
    {
        // Pool of reusable SocketAsyncEventArgs objects.        
        Stack<SocketAsyncEventArgs> pool;

        // initializes the object pool to the specified size.
        // "capacity" = Maximum number of SocketAsyncEventArgs objects
        public SocketAsyncEventArgsStack(Int32 capacity)
        {
            this.pool = new Stack<SocketAsyncEventArgs>(capacity);
        }

        // The number of SocketAsyncEventArgs instances in the pool.         
        public Int32 Count
        {
            get { return this.pool.Count; }
        }

        // Removes a SocketAsyncEventArgs instance from the pool.
        // returns SocketAsyncEventArgs removed from the pool.
        public SocketAsyncEventArgs Pop()
        {
            lock (this.pool)
            {
                SocketAsyncEventArgs args = null;
                if (pool.Count > 0)
                {
                    args = this.pool.Pop();
                }
                else
                {
                    args = new SocketAsyncEventArgs();
                    byte[] buffer = new byte[1024];

                    args.SetBuffer(buffer, 0, buffer.Length);

                    SockState recState = new SockState(args, buffer.Length, null);
                    recState.IsCached = false;
                    recState.ID = Interlocked.Decrement(ref SockStateID);
                    args.UserToken = recState;
                    recState.BufferBlockOffset = 0;
                    recState.BufferBlockLength = 1024;
                }
                return args;
            }
        }

        /// <summary>
        /// Ad-hoc SocketState IDs
        /// </summary>
        private static int SockStateID = int.MaxValue;

        // Add a SocketAsyncEventArg instance to the pool. 
        // "item" = SocketAsyncEventArgs instance to add to the pool.
        public void Push(SocketAsyncEventArgs item)
        {
            if (item == null) 
            { 
                throw new ArgumentNullException("Items added to a SocketAsyncEventArgsPool cannot be null"); 
            }

            SockState state = item.UserToken as SockState;
#if !SILVERLIGHT
                item.AcceptSocket = null;
#endif
            if (state != null)
            {
                state.Reset();
            }

            if (!state.IsCached)
            {
                return;
            }

            lock (this.pool)
            {                
                this.pool.Push(item);
            }
        }


    }
}
