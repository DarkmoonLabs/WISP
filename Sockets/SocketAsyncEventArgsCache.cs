using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace Shared
{
    /// <summary>
    /// Handles caching of SocketAsyncEventArg objects.  By caching them in a continuous block of memory, we prevent long-term memory fragmentation under high load
    /// that dynamic allocation would produce.  As a side effect, this class acts as a throttle to how much network activity we attempt.
    /// </summary>
    public class SocketAsyncEventArgsCache
    {
        static SocketAsyncEventArgsCache()
        {
            m_PoolOfReadEventArgs = new SocketAsyncEventArgsStack(0);
            m_PoolOfAcceptEventArgs = new SocketAsyncEventArgsStack(0);
            m_PoolOfSendEventArgs = new SocketAsyncEventArgsStack(0);
        }

        private static BufferManager m_Buffer;
          
        // pool of reusable SocketAsyncEventArgs objects for receive operations
        private static SocketAsyncEventArgsStack m_PoolOfReadEventArgs;

        // pool of reusable SocketAsyncEventArgs objects for send operations
        private static SocketAsyncEventArgsStack m_PoolOfSendEventArgs;

        // pool of reusable SocketAsyncEventArgs objects for accept operations
        private static SocketAsyncEventArgsStack m_PoolOfAcceptEventArgs;

        public static byte[] BufferBlock
        {
            get
            {
                return m_Buffer.BufferBlock;
            }
        }

        public static string ReportPoolStatus()
        {
            if (m_PoolOfAcceptEventArgs != null && m_PoolOfSendEventArgs != null && m_PoolOfReadEventArgs != null)
            {
                return string.Format("SocketAsynchEvent Arg Pool Status: AcceptArgs: {0}, SendArgs: {1}, ReceiveArgs: {2}", m_PoolOfAcceptEventArgs.Count, m_PoolOfSendEventArgs.Count, m_PoolOfReadEventArgs.Count);
            }
            else
            {
                return "";
            }
        }

        public static bool IsInitialized = false;
        public static void Init(int bufferSize, int maxClients, int maxAcceptSockets)
        {
            maxClients *= 2; // each client has two sockets

            if (IsInitialized)
            {
                return;
            }

            IsInitialized = true;

            int total = bufferSize * ((maxClients * 2) + maxAcceptSockets + 3);
            m_Buffer = new BufferManager(total, bufferSize);

            m_PoolOfReadEventArgs = new SocketAsyncEventArgsStack(maxClients + 1);
            m_PoolOfAcceptEventArgs = new SocketAsyncEventArgsStack(maxAcceptSockets + 1);
            m_PoolOfSendEventArgs = new SocketAsyncEventArgsStack(maxClients + 1);

            // Allocate one large byte buffer block, which all I/O operations will 
            //use a piece of. This gaurds against memory fragmentation.
            m_Buffer.InitBuffer();            

            // Let's make 1 extra SAEA to spare for each operation. If we do that, then we have to
            // consider it when we specify the buffer block's size in SocketListener
            // constructor.

            int[] maxOps = new int[] { maxClients+1, maxClients+1, maxAcceptSockets+1 };
            SocketAsyncEventArgsStack[] pools = new SocketAsyncEventArgsStack[] { m_PoolOfSendEventArgs, m_PoolOfReadEventArgs, m_PoolOfAcceptEventArgs };

            for (int x = 0; x < maxOps.Length; x++)
            {
                for (int i = 0; i < maxOps[x]; i++)
                {
                    SocketAsyncEventArgs args = new SocketAsyncEventArgs();

                    // assign a byte buffer from the buffer block to 
                    //this particular SocketAsyncEventArg object
                    m_Buffer.SetBuffer(args);

                    SockState recState = new SockState(args, bufferSize, pools[x]);
                    recState.ID = i+1;
                    args.UserToken = recState;
                    recState.BufferBlockOffset = args.Offset;

                    // add this SocketAsyncEventArg object to the pool.                    
                    pools[x].Push(args);
                }
            }

        }

        public static SocketAsyncEventArgs PopReadEventArg(EventHandler<SocketAsyncEventArgs> newIOCompletedHandler, Socket socket)
        {
            SocketAsyncEventArgs args = m_PoolOfReadEventArgs.Pop();
#if  !SILVERLIGHT
            args.AcceptSocket = socket;
#endif
            args.Completed += newIOCompletedHandler;
            //Log.LogMsg("==> Popped read arg #" + ((SockState)args.UserToken).ID.ToString() + "#. There are " + m_PoolOfReadEventArgs.Count.ToString() + " left");
            return args;
        }

        public static SocketAsyncEventArgs PopSendEventArg(EventHandler<SocketAsyncEventArgs> newIOCompletedHandler, Socket socket)
        {
            SocketAsyncEventArgs args = m_PoolOfSendEventArgs.Pop();
#if  !SILVERLIGHT
            args.AcceptSocket = socket;
#endif
            args.Completed += newIOCompletedHandler;
            //Log.LogMsg("==> Popped send arg #" + ((SockState)args.UserToken).ID.ToString() + "#.  There are " + m_PoolOfSendEventArgs.Count.ToString() + " left");
            return args;
        }

        public static SocketAsyncEventArgs PopAcceptEventArg(EventHandler<SocketAsyncEventArgs> newIOCompletedHandler, Socket socket)
        {
            SocketAsyncEventArgs args = m_PoolOfAcceptEventArgs.Pop();
#if  !SILVERLIGHT
            args.AcceptSocket = socket;
#endif
            args.Completed += newIOCompletedHandler;
            //Log.LogMsg("==> Popped accept arg #" + ((SockState)args.UserToken).ID.ToString() + "#.  There are " + m_PoolOfAcceptEventArgs.Count.ToString() + " left");
            return args;
        }

        public static void PushReadEventArg(SocketAsyncEventArgs args, EventHandler<SocketAsyncEventArgs> oldIOCompletedHandler)
        {
            args.Completed -= oldIOCompletedHandler;
            if (m_PoolOfReadEventArgs != null)
            {
                m_PoolOfReadEventArgs.Push(args);
               //Log.LogMsg("==> Pushed read arg #" + ((SockState)args.UserToken).ID.ToString() + "#.  There are " + m_PoolOfReadEventArgs.Count.ToString() + " left");
            }
            else
            {
#if !SILVERLIGHT
                args.AcceptSocket = null;
#endif
                SockState state = args.UserToken as SockState;
                if (state != null)
                {
                    state.Reset();
                }
            }            
        }

        public static void PushSendEventArg(SocketAsyncEventArgs args, EventHandler<SocketAsyncEventArgs> oldIOCompletedHandler)
        {
            args.Completed -= oldIOCompletedHandler;
            if (m_PoolOfSendEventArgs != null)
            {
                m_PoolOfSendEventArgs.Push(args);
                //Log.LogMsg("==> Pushed send arg #" + ((SockState)args.UserToken).ID.ToString() + "#.  There are " + m_PoolOfSendEventArgs.Count.ToString() + " left");
            }
            else
            {
#if !SILVERLIGHT
                args.AcceptSocket = null;
#endif
                SockState state = args.UserToken as SockState;
                if (state != null)
                {
                    state.Reset();
                }
            }            
        }

        public static void PushAcceptEventArg(SocketAsyncEventArgs args, EventHandler<SocketAsyncEventArgs> oldIOCompletedHandler)
        {
            args.Completed -= oldIOCompletedHandler;
            if (m_PoolOfAcceptEventArgs != null)
            {
                m_PoolOfAcceptEventArgs.Push(args);
                //Log.LogMsg("==> Pushed accept arg #" + ((SockState)args.UserToken).ID.ToString() + "#.  There are " + m_PoolOfAcceptEventArgs.Count.ToString() + " left");
            }
            else
            {
#if !SILVERLIGHT
                args.AcceptSocket = null;
#endif
                SockState state = args.UserToken as SockState;
                if (state != null)
                {
                    state.Reset();
                }
            }            
        }


    }
}
