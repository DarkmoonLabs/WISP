using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using Shared;
using System.Threading;

namespace Shared
{
    /// <summary>
    /// Sends & receives a socket's data in an asynchronous fashion using kernel level I/O Completion Ports (IOCMP).    
    /// </summary>
    public class DuplexAsynchTransitStrategy : ITransitStrategy
    {
        public DuplexAsynchTransitStrategy(INetworkConnection con)
        {
            m_OwningConnection = con;
            LastUDPACKReceived = DateTime.MinValue;
        }

        private INetworkConnection m_OwningConnection;
        public INetworkConnection OwningConnection
        {
            get { return m_OwningConnection; }
            set { m_OwningConnection = value; }
        }

        /*  Some notes on Async socket ops
       * 
       * - Socket.*Async() ops map to I/O completion ports in the system
       * - Submitting multiple packet to the completion port queue may result in those packets being sent in a non-linera fashion
       * - Don't submit more than one packet to the I/O completion port queue.  Don't submit the next packet until the first one is completed
       * - If we have a new packet ready to go, and the previous op hasn't completed, we must queue the packet ourselves
       * - multiple simultaneous receives on the same socket is a dumb idea too. As such, each connection only needs
       *   , at maximum, one receive event arg and one send event arg.  Pull these out of the pool when the connection is created
       *   and put them back when it's killed
       * 
       */

        private SocketAsyncEventArgs m_RecArgs = null;
        private SocketAsyncEventArgs m_SendArgs = null;
        private SocketAsyncEventArgs m_SendArgsUDP = null;

        /// <summary>
        /// The number of packet acknowledgements we're waiting for
        /// </summary>
        private int m_NumAcksWaitingFor = 0;
        public int NumAcksWaitingFor
        {
            get { return m_NumAcksWaitingFor; }
        }        

        public DateTime LastUDPACKReceived { get; set; }

        private long m_Sending = 0; // synchronization flag
        private Queue<NetQItem> m_SendQueue = new Queue<NetQItem>(); // packet send queue

        private long m_SendingUDP = 0; // synchronization flag
        private Queue<NetQItem> m_SendQueueUDP = new Queue<NetQItem>(); // packet send queue


        public void OnPacketReceiptACK(PacketACK msg)
        {
            m_NumAcksWaitingFor--;
            if (msg.IsUDP)
            {
                LastUDPACKReceived = DateTime.UtcNow;
                //Log.LogMsg("Got UDP ACK from " + RemoteEndPoint.ToString());
            }
        }

        /// <summary>
        /// Serializes and sends a packet across the wire
        /// </summary>
        /// <param name="msg">The packet to send</param>
        public virtual int Send(Packet msg)
        {
            try
            {
                if (msg.NeedsDeliveryAck)
                {
                    m_NumAcksWaitingFor++;
                }
                //// Log.LogMsg("Testy 1");
                byte[] raw = OwningConnection.SerializePacket(msg);
                //// Log.LogMsg("Testy 2");
                //Log.LogMsg("Sending -> " + msg.GetType().ToString() + " (" + raw.Length.ToString() + " bytes)");
                //Log.LogMsg("   That message is " + raw.Length.ToString() + " bytes long");
                return Send(raw, msg.Flags);
            }
            catch (Exception sendExc)
            {
                OwningConnection.KillConnection(sendExc.Message);
                return -1; ;
            }
        }

        /// <summary>
        /// Sends arbitrary bytes of data across the wire.  Note that if the remote endpoint can't decipher the data
        /// as a known packet, the connection will be dropped by that endpoint immediately.  
        /// </summary>
        /// <param name="data">bytes to send</param>
        public virtual int Send(byte[] data, PacketFlags flags)
        {
            //Log.LogMsg("@__@ Monitor Enter");
            if (!OwningConnection.IsAlive)
            {
                //// Log.LogMsg("Testy 3");
                return -1;
            }

            //Log.LogMsg("==>Sending " + data.Length.ToString() + " bytes.");
            try
            {
                bool isUDP = (flags & PacketFlags.UDP) != 0;

#if SILVERLIGHT
                // Silverlight can't do UDP and it can only send Async, i.e. Non-Blocking
                isUDP = false;
                OwningConnection.BlockingMode = false;
#endif
                if (isUDP)
                {
                    //// Log.LogMsg("Testy 4");
                    if (!OwningConnection.CanSendUDP)
                    {
                        Log.LogMsg("!!! Tried sending UDP packet when the connection wasn't yet ready to send UDP. Dropping packet.");
                        return 0;
                    }

                    // Don't allow fragmenting UDP packets
                    if (data.Length > m_SendArgsUDP.Buffer.Length && data.Length > 1024)
                    {
                        Log.LogMsg("Message exceeded UDP size. Sending via TCP instead.");
                        flags &= ~PacketFlags.UDP;
                    }
                }

                if (!OwningConnection.BlockingMode)
                {
                    // send via asyncsend, but only one send op can be in progress at one time
                    // out of order packets can happen because the actual send is done with I/O completion ports and if multiple
                    // packets get submitted to the I/O queue they could be processed out of order, especially if the packet are of wildly differing sizes
                    if (isUDP)
                    {
                        //// Log.LogMsg("Testy 5");
                        lock (m_SendQueueUDP)
                        {
                            if (1 == Interlocked.Exchange(ref m_SendingUDP, 1))
                            {
                                // failed to get the lock - a message is in progress.  queue message.
                                NetQItem qi = new NetQItem();
                                qi.Flags = flags;
                                qi.Data = data;
                                m_SendQueueUDP.Enqueue(qi);
                                Log.LogMsg("Queueing UDP packet. Now " + m_SendQueueUDP.Count.ToString() + " in Queue.");
                                return 1;
                            }
                            //Log.LogMsg("Acquired UDP send lock.");
                        }
                    }
                    else
                    {
                        lock (m_SendQueue)
                        {
                            if (1 == Interlocked.Exchange(ref m_Sending, 1))
                            {
                                // failed to get the lock - a message is in progress.  queue message.
                                NetQItem qi = new NetQItem();
                                qi.Flags = flags;
                                qi.Data = data;
                                m_SendQueue.Enqueue(qi);
                                Log.LogMsg("Queueing TCP packet. Now " + m_SendQueue.Count.ToString() + " in Queue.");
                                return 1;
                            }
                            //Log.LogMsg("Acquired TCP send lock.");
                        }
                    }
                }

                SocketAsyncEventArgs args = isUDP ? m_SendArgsUDP : m_SendArgs;
                SockState state = args.UserToken as SockState;
                state.Flags = flags;
                state.PacketBuffer = data;
                //// Log.LogMsg("Testy 6");
                SendBuffer(args, state, isUDP);
            }
            catch (Exception sendExc)
            {
                OwningConnection.KillConnection(sendExc.Message);
                return -1; ;
            }

            return 1;
        }     

        /// <summary>
        /// We dont usually send everything all at once in the buffer.  This method sends what's in the buffer out in a piece by piece fashion.
        /// </summary>
        private void SendBuffer(SocketAsyncEventArgs args, SockState state, bool isUDP)
        {
            try
            {
#if !SILVERLIGHT
                //Log.LogMsg("==>$$$$ Async SEND op started - #" + ((SockState)args.UserToken).ID.ToString() + "#");
                if (!OwningConnection.BlockingMode)
#endif
                {
                    int toSend = state.PacketBuffer.Length - state.PacketBufferPointer.Position;
                    if (toSend > state.BufferBlockLength)
                    {
                        toSend = state.BufferBlockLength;
                    }

                    args.SetBuffer(state.BufferBlockOffset, toSend);
                    Util.Copy(state.PacketBuffer, state.PacketBufferPointer.Position, args.Buffer, state.BufferBlockOffset, toSend);

                    Socket socket = OwningConnection.MyTCPSocket;
#if !SILVERLIGHT
                    if (isUDP)
                    {
                        socket = OwningConnection.MyUDPSocket;
                        //Log.LogMsg("UDP Send Target = " + SendTarget.ToString());
                        args.RemoteEndPoint = OwningConnection.UDPSendTarget;
                        if (!socket.SendToAsync(args))
                        {
                            //// Log.LogMsg("Testy 7");
                            OnSendResolved(args, state);
                        }
                    }
                    else
#endif
                    {
                        if (!socket.SendAsync(args))
                        {
                            //// Log.LogMsg("Testy 8");
                            OnSendResolved(args, state);
                        }
                    }
                }
#if !SILVERLIGHT
                else
                {
                    if (isUDP)
                    {
                        //// Log.LogMsg("Testy 9");
                        OwningConnection.MyUDPSocket.SendTo(state.PacketBuffer, OwningConnection.UDPSendTarget);
                    }
                    else
                    {
                        //// Log.LogMsg("Testy 10");
                        OwningConnection.MyTCPSocket.Blocking = true;
                        OwningConnection.MyTCPSocket.Send(state.PacketBuffer);
                    }

                    //// Log.LogMsg("Testy 11");
                    OwningConnection.SentBytes(state.PacketBuffer);
                    OwningConnection.PacketSent();
                }
#endif
            }
            catch (Exception e)
            {
                //// Log.LogMsg("Testy 12");
                Log.LogMsg("Error SendBuffer. " + e.Message);
                OwningConnection.KillConnection("Send error. " + e.Message);
            }
        }

        private void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            try
            {
                // determine which type of operation just completed and call the associated handler
                switch (e.LastOperation)
                {
                    case SocketAsyncOperation.Receive:
                        OnReceiveResolved(e);
                        break;
                    case SocketAsyncOperation.Send:
#if !SILVERLIGHT
                    case SocketAsyncOperation.SendTo:
#endif
                        //Log.LogMsg("Send EVENT completed for @" + ((SockState)e.UserToken).ID.ToString() + "@");
                        OnSendResolved(e, e.UserToken as SockState);
                        break;
                    default:
                        //This exception will occur if you code the Completed event of some
                        //operation to come to this method, by mistake.
                        throw new ArgumentException("The last operation completed on the socket was not a receive or send");
                }
            }
            catch (Exception ex)
            {
                Log.LogMsg("Error in IO_Completed. " + ex.Message);
                OwningConnection.KillConnection("Error I/O. " + ex.Message);
            }
        }

        /// <summary>
        /// Gets called when a send operation resolves.
        /// </summary>
        private void OnSendResolved(SocketAsyncEventArgs args, SockState state)
        {
            //// Log.LogMsg("Testy 13");
            try
            {
                OwningConnection.SentBytes(state.PacketBufferPointer.Position);
                bool isUDP = (state.Flags & PacketFlags.UDP) != 0;
                //Log.LogMsg("==>#### Async SEND Op Completed - #" + ((SockState)args.UserToken).ID.ToString() + "#");
                if (args.SocketError == SocketError.Success)
                {
                    //// Log.LogMsg("Testy 14");
                    state.PacketBufferPointer.Advance(args.BytesTransferred);
                    if (state.PacketBufferPointer.Position >= state.PacketBuffer.Length)
                    {
                        OwningConnection.PacketSent();
                        // Done sending packet.
                        state.Reset();
                        //Log.LogMsg("==>Done sending packet. Sent " + state.PacketBufferPointer.Position.ToString() + " bytes.");

                        // done sending packet, see if we have anything in the queue ready to go
                        bool more = false;
                        Queue<NetQItem> sendQ = isUDP ? m_SendQueueUDP : m_SendQueue;
                        lock (sendQ)
                        {
                            if (sendQ.Count > 0)
                            {
                                NetQItem itm = sendQ.Dequeue();
                                state.PacketBuffer = itm.Data;
                                state.Flags = itm.Flags;
                                more = true;
                            }
                            else
                            {
                                // release the sending lock               
                                if (isUDP)
                                {
                                    //Log.LogMsg("UDP send queue emptied.");
                                    Interlocked.Exchange(ref m_SendingUDP, 0);
                                }
                                else
                                {
                                    //Log.LogMsg("TCP send queue emptied.");
                                    Interlocked.Exchange(ref m_Sending, 0);
                                }
                            }
                        }

                        if (more)
                        {
                            //// Log.LogMsg("Testy 15");
                            SendBuffer(args, state, isUDP);
                        }

                        return;
                    }
                    else
                    {
                        //// Log.LogMsg("Testy 16");
                        // not done sending.  send again.
                        //Log.LogMsg("==>Continuing send.  " + state.PacketBufferPointer.Position.ToString() + " / " + state.PacketBuffer.Length.ToString() + " sent so far.");
                        SendBuffer(args, state, isUDP);
                    }
                }
                else
                {
                    //If we are in this else-statement, there was a socket error.                    
                    OwningConnection.KillConnection("Error sending packet. " + args.SocketError.ToString());
                }
            }
            catch (Exception ex)
            {
                //// Log.LogMsg("Testy 17");
                Log.LogMsg("Failed to ProcessSend. " + ex.Message);
                OwningConnection.KillConnection("Send error. " + ex.Message);
            }
        }

        /// <summary>
        /// Begin the async reading process
        /// </summary>
        /// <param name="args"></param>
        public bool ListenForDataOnSocket()
        {
            // Log.LogMsg("Testy 18");
            if (!OwningConnection.IsConnected)
            {
                // Log.LogMsg("Testy 19");
                int x = 0;
                return false;
            }

            bool willRaiseEvent = false;
            // Post async receive operation on the socket.
            // Log.LogMsg("==><<<< TCP Async RECEIVE op started - #" + ((SockState)m_RecArgs.UserToken).ID.ToString() + "#");            
            willRaiseEvent = OwningConnection.MyTCPSocket.ReceiveAsync(m_RecArgs);
            if (!willRaiseEvent)
            {
                // Log.LogMsg("Testy 20");
                OnReceiveResolved(m_RecArgs);
            }

            return true;
        }

        /// <summary>
        /// Gets called when a receive operation resolves.  If we were listening for data and the connection
        /// closed, that also counts as a receive operation resolving.
        /// </summary>
        /// <param name="args"></param>
        private void OnReceiveResolved(SocketAsyncEventArgs args)
        {
            // Log.LogMsg("Testy 21");
            // Log.LogMsg("==>++++ Async RECEIVE Op Completed - #" + ((SockState)args.UserToken).ID.ToString() + "#");
            try
            {
                if (!OwningConnection.IsAlive && args.BytesTransferred > 0)
                {
                    // Log.LogMsg("Testy 22");
                    return;
                }

                SockState state = args.UserToken as SockState;
                // If there was a socket error, close the connection. This is NOT a normal
                // situation, if you get an error here.
                if (args.SocketError != SocketError.Success)
                {
                    // Log.LogMsg("Testy 222");
                    OwningConnection.KillConnection("Connection lost! Network receive error: " + args.SocketError);
                    //Jump out of the ProcessReceive method.
                    return;
                }

                // If no data was received, close the connection. This is a NORMAL
                // situation that shows when the client has finished sending data.
                if (args.BytesTransferred == 0)
                {
                    // Log.LogMsg("Testy 223");
                    OwningConnection.KillConnection("Connection closed by remote host.");
                    return;
                }

                //// Log.LogMsg("Testy 224");
                OwningConnection.ReceivedBytes(args.BytesTransferred);
                // restart listening process                
                //// Log.LogMsg("Testy 225");
                OwningConnection.AssembleInboundPacket(args, state);
                //// Log.LogMsg("Testy 226");
                ListenForDataOnSocket();
            }
            catch (Exception ex)
            {
                //// Log.LogMsg("Testy 23");
                Log.LogMsg("Error ProcessReceive. " + ex.Message);
                OwningConnection.KillConnection("Error receive. " + ex.Message);
            }
        }

        public void InitTCP()
        {
            if(OwningConnection.MyTCPSocket != null)
            {
                m_RecArgs = SocketAsyncEventArgsCache.PopReadEventArg(new EventHandler<SocketAsyncEventArgs>(IO_Completed), OwningConnection.MyTCPSocket);
                m_SendArgs = SocketAsyncEventArgsCache.PopSendEventArg(new EventHandler<SocketAsyncEventArgs>(IO_Completed), OwningConnection.MyTCPSocket);
            }
            else
            {
                throw new ArgumentException("AsynchTransitStrategy can't initialize TCP until the NetworkConnection has a valid TCPSocket. Currently TCPSocket is null.");
            }
        }

        public void InitUDP()
        {
            if (OwningConnection.MyTCPSocket != null)
            {
                m_SendArgsUDP = SocketAsyncEventArgsCache.PopSendEventArg(new EventHandler<SocketAsyncEventArgs>(IO_Completed), OwningConnection.MyUDPSocket);
            }
            else
            {
                throw new ArgumentException("AsynchTransitStrategy can't initialize UDP until the NetworkConnection has a valid UDPSocket. Currently UDPSocket is null.");
            }            
        }

        public void BeforeShutdown()
        {
            lock (m_SendQueue)
            {
                m_SendQueue.Clear();
            }

            lock (m_SendQueueUDP)
            {
                m_SendQueueUDP.Clear();
            }
        }

        public bool HasQueuedPackets
        {
            get
            {
                lock (m_SendQueue)
                {
                    lock (m_SendQueueUDP)
                    {
                        bool haveQ = m_SendQueue != null && m_SendQueue.Count > 0;
                        bool haveUQ = m_SendQueueUDP != null && m_SendQueueUDP.Count > 0;
                        return haveQ | haveUQ;
                    }
                }
            }
        }

        public void AfterShutdown()
        {
            if (m_RecArgs != null)
            {
                SocketAsyncEventArgsCache.PushReadEventArg(m_RecArgs, new EventHandler<SocketAsyncEventArgs>(IO_Completed));
                m_RecArgs = null;
            }

            if (m_SendArgs != null)
            {
                SocketAsyncEventArgsCache.PushSendEventArg(m_SendArgs, new EventHandler<SocketAsyncEventArgs>(IO_Completed));
                m_SendArgs = null;
            }

            if (m_SendArgsUDP != null)
            {
                SocketAsyncEventArgsCache.PushSendEventArg(m_SendArgsUDP, new EventHandler<SocketAsyncEventArgs>(IO_Completed));
                m_SendArgsUDP = null;
            }
        }


        public void ProcessSend()
        {
        }

        public bool ProcessReceive()
        {
            return true;
        }
    }
}
