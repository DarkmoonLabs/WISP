using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;

namespace Shared
{
    /// <summary>
    /// Simple threaded network transfer algorithm.  You must call StopNetworkingPump() to release the threads for this object after you are done using it.
    /// </summary>
    public class SimplexAsyncTransitStrategy : ITransitStrategy
    {
        public SimplexAsyncTransitStrategy(INetworkConnection con)
        {
            m_OwningConnection = con;
            LastUDPACKReceived = DateTime.MinValue;
            m_TCPSockState = new SockState(null, 0, null);
            m_TCPSockState.AsyncEventArgs = new SocketAsyncEventArgs();
        }

        private INetworkConnection m_OwningConnection;
        public INetworkConnection OwningConnection
        {
            get { return m_OwningConnection; }
            set { m_OwningConnection = value; }
        }

        public DateTime LastUDPACKReceived { get; set; }
        private byte[] m_TCPBuffer = new byte[1024];
        private int m_TCPBytesReceived = 0;
        private SockState m_TCPSockState;
        private SocketError m_LastTCPReceiveStatus = SocketError.NotInitialized;
        private bool m_Shutdown = false;
        private Thread m_SendThread;
        private Thread m_ReceiveThread;
        private object m_ThreadSync = new object();


        public void StopNetworkingPump()
        {
            m_Shutdown = true;
         
            lock (m_ThreadSync)
            {
                if (Thread.CurrentThread == m_ReceiveThread)
                {
                    try
                    {
                        if (m_SendThread != null)
                        {
                            Log.LogMsg("Abort network send thread.");
                            m_SendThread.Abort();
                            m_SendThread = null;
                        }
                    }
                    catch { }

                    try
                    {
                        if (m_ReceiveThread != null)
                        {
                            Log.LogMsg("Abort TCP receive thread.");
                            m_ReceiveThread.Abort();
                            m_ReceiveThread = null;
                        }
                    }
                    catch { }
                }
                else
                {
                    try
                    {
                        if (m_ReceiveThread != null)
                        {
                            Log.LogMsg("Abort TCP receive thread.");
                            m_ReceiveThread.Abort();
                            m_ReceiveThread = null;
                        }
                    }
                    catch { }

                    try
                    {
                        if (m_SendThread != null)
                        {
                            Log.LogMsg("Abort network send thread.");
                            m_SendThread.Abort();
                            m_SendThread = null;
                        }
                    }
                    catch { }
                }


                
            }
        }

        private void DoSend()
        {
            try
            {
                while (!m_Shutdown)
                {
                    NetQItem item;
                    lock (m_SendQueue)
                    {
                        while (m_SendQueue.Count == 0)
                        {
                            // Acquire a lock on the queue so we don't contend for the same item with another thread in our pool
                            Monitor.Wait(m_SendQueue);
                        }

                        item = m_SendQueue.Dequeue();
                    }

                    SocketDebug(1);
                    // Send
                    SendBytes(item.IsUDP, item.Data);
                    SocketDebug(2);
                }

                // We can only get here if the queue is being shut down forcibly, so we purge the queue.
                m_SendQueue.Clear();
            }
            catch (ThreadAbortException abort)
            {
            }
            catch
            {
            }
        }

        private void SendBytes(bool isUDP, byte[] data)
        {
             try
            {
                if (!OwningConnection.IsConnected)
                {
                    return;
                }

                if (isUDP)
                {
                    SocketDebug(777);
                    OwningConnection.MyUDPSocket.SendTo(data, OwningConnection.UDPSendTarget);
                }
                else
                {
                    SocketDebug(3);
                    OwningConnection.MyTCPSocket.Blocking = true;
                    OwningConnection.MyTCPSocket.Send(data);
                    SocketDebug(4);
                }

                OwningConnection.SentBytes(data);
                OwningConnection.PacketSent();
            }
            catch (Exception e)
            {
                Log.LogMsg("Error SendBytes. " + e.Message);
                OwningConnection.KillConnection("Send error. " + e.Message);
            }
        }
        
        private void DoListen()
        {
            try
            {
                while (!m_Shutdown)
                {
                    ListenTCP();
                }
            }
            catch (ThreadAbortException abort)
            {
            }
            catch
            {
            }
        }

        /// <summary>
        /// Begin the async reading process
        /// </summary>
        /// <param name="args"></param>
        public bool ListenForDataOnSocket()
        {
            if (!OwningConnection.IsConnected)
            {
                SocketDebug(5);
                return false;
            }

            if (m_ReceiveThread == null)
            {
                lock (m_ThreadSync)
                {
                    m_ReceiveThread = new Thread(new ThreadStart(DoListen));
                }
                m_ReceiveThread.IsBackground = true;
                m_ReceiveThread.Name = "TCP Receive Thread #" + m_ReceiveThread.GetHashCode();
                m_ReceiveThread.Start();
            }
            
            return true;
        }

        /// <summary>
        /// Blocking receive TCP call.
        /// </summary>
        /// <returns></returns>
        private bool ListenTCP()
        {
            if (!OwningConnection.IsConnected)
            {
                SocketDebug(6);
                return false;
            }

            SocketDebug(7);
            OwningConnection.MyTCPSocket.Blocking = true;
            m_TCPBytesReceived = OwningConnection.MyTCPSocket.Receive(m_TCPBuffer, 0, m_TCPBuffer.Length, SocketFlags.None, out m_LastTCPReceiveStatus);
            SocketDebug(8);
            OnReceiveResolved(false, m_TCPBuffer, m_TCPBytesReceived, m_LastTCPReceiveStatus, m_TCPSockState);
            SocketDebug(9);
            return true;    
        }

        public void SocketDebug(int num)
        {
           // string error = "";// OwningConnection.MyTCPSocket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Error).ToString();
            //Log.LogMsg("[" + num.ToString() + "] Socket.Handle=" + OwningConnection.MyTCPSocket.Handle.ToString() + ", IsAlive=" + OwningConnection.MyTCPSocket.Connected.ToString() + ", LastTCPReceiveStatus " + m_LastTCPReceiveStatus.ToString() + ", LastError=" + error);
        }

        /// <summary>
        /// Gets called when a receive operation resolves.  If we were listening for data and the connection
        /// closed, that also counts as a receive operation resolving.
        /// </summary>
        /// <param name="args"></param>
        private void OnReceiveResolved(bool isUDP, byte[] buffer, int bytesReceived, SocketError status, SockState sockState)
        {
            SocketDebug(10);
            //// Log.LogMsg("==>++++ Async RECEIVE Op Completed - #" + ((SockState)args.UserToken).ID.ToString() + "#");
            try
            {
                if (!OwningConnection.IsAlive && bytesReceived > 0)
                {
                    return;
                }

                // If there was a socket error, close the connection. This is NOT a normal
                // situation, if you get an error here.
                if (status != SocketError.Success)
                {
                    if (!OwningConnection.ShuttingDown)
                    {
                        SocketDebug(11);
                        OwningConnection.KillConnection("Connection lost! Network receive error: " + status);
                    }
                    //Jump out of the ProcessReceive method.
                    return;
                }

                SocketDebug(12);
                m_TCPSockState.AsyncEventArgs.RemoteEndPoint = OwningConnection.MyTCPSocket.RemoteEndPoint;

                // If no data was received, close the connection. This is a NORMAL
                // situation that shows when the client has finished sending data.
                if (bytesReceived == 0)
                {
                    if (!OwningConnection.ShuttingDown)
                    {
                        OwningConnection.KillConnection("Connection closed by remote host.");
                    }
                    return;
                }

                OwningConnection.ReceivedBytes(bytesReceived);

                // restart listening process                
                OwningConnection.AssembleInboundPacket(buffer, bytesReceived, sockState);
            }
            catch (ThreadAbortException abort)
            {
            }
            catch (Exception ex)
            {
                Log.LogMsg("Error ProcessReceive. " + ex.Message);
                OwningConnection.KillConnection("Error receive. " + ex.Message);
            }
        }

        protected virtual void TimestampOutgoingPacket(Packet msg)
        {
            msg.SentOnUTC = DateTime.UtcNow.Ticks;
        }
   
        /// <summary>
        /// The number of packet acknowledgements we're waiting for
        /// </summary>
        private int m_NumAcksWaitingFor = 0;
        public int NumAcksWaitingFor
        {
            get { return m_NumAcksWaitingFor; }
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

                byte[] raw = OwningConnection.SerializePacket(msg);                
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

        private int m_LastTCPPacketIdProcessed = 0;
        protected Queue<NetQItem> m_SendQueue = new Queue<NetQItem>(); // packet send queue
        
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
                return -1;
            }

            //Log.LogMsg("==>Sending " + data.Length.ToString() + " bytes.");
            try
            {
                bool isUDP = (flags & PacketFlags.UDP) != 0;
                if (isUDP)
                {
                    if (!OwningConnection.CanSendUDP)
                    {
                        Log.LogMsg("!!! Tried sending UDP packet when the connection wasn't yet ready to send UDP. Dropping packet.");
                        return 0;
                    }

                    // Don't allow fragmenting UDP packets
                    if (data.Length > 1024)
                    {
                        Log.LogMsg("Message exceeded UDP size of 1024 bytes. Sending via TCP instead.");
                        flags &= ~PacketFlags.UDP;
                    }                    
                }
               
                lock (m_SendQueue)
                {
                    NetQItem qi = new NetQItem();
                    qi.Flags = flags;
                    qi.Data = data;
                    qi.IsUDP = isUDP;
                    m_SendQueue.Enqueue(qi);
                    
                    Monitor.Pulse(m_SendQueue);
                    return 1;
                }

            }
            catch (Exception sendExc)
            {
                return -1; ;
            }

            return 1;
        }

        public void InitTCP()
        {            
            OwningConnection.MyTCPSocket.Blocking = true;
            lock (m_ThreadSync)
            {
                m_SendThread = new Thread(new ThreadStart(DoSend));
            }
            m_SendThread.IsBackground = true;
            m_SendThread.Name = "Network Send Thread #" + m_SendThread.GetHashCode();
            m_SendThread.Start();
        }

        public void InitUDP()
        {        
        }

        public void BeforeShutdown()
        {            
        }

        public void AfterShutdown()
        {
            if (m_ReceiveThread == Thread.CurrentThread || m_SendThread == Thread.CurrentThread)
            {
                return;
            }
            StopNetworkingPump();
        }

        public void OnPacketReceiptACK(PacketACK msg)
        {
            m_NumAcksWaitingFor--;
            if (msg.IsUDP)
            {
                LastUDPACKReceived = DateTime.UtcNow;
                //Log.LogMsg("Got UDP ACK from " + RemoteEndPoint.ToString());
            }
        }
        
        public bool HasQueuedPackets
        {
            get
            {
                lock (m_SendQueue)
                {
                    bool haveQ = m_SendQueue != null && m_SendQueue.Count > 0;
                    return haveQ;
                }
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
