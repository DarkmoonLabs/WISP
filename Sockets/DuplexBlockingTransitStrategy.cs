using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace Shared
{
    /// <summary>
    /// Unthreaded network transit, suitable for use in tightly looped game clients. Both sends and receives are blocking and synchronous.
    /// This class is not thread safe.
    /// </summary>
    public class DuplexBlockingTransitStrategy : ITransitStrategy
    {
        public DuplexBlockingTransitStrategy(INetworkConnection con)
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
        private byte[] m_TCPBuffer = new byte[4096];
        private int m_TCPBytesReceived = 0;
        private SockState m_TCPSockState;
        private SocketError m_LastTCPReceiveStatus = SocketError.NotInitialized;

        public void ProcessSend()
        {
            try
            {
                SocketDebug(1);
                // Log.LogMsg("ProcessSend at " + DateTime.Now.Ticks.ToString());
                NetQItem item;
                while (m_SendQueue.Count > 0)
                {
                    SocketDebug(2);
                    item = m_SendQueue.Dequeue();
                    SocketDebug(3);                    
                    SendBytes(item.IsUDP, item.Data);
                    SocketDebug(4);
                }
            }
            catch(Exception e)
            {
                Log.LogMsg("DuplexBlockingTransit Failed to ProcessSend. " + e.Message);
            }
        }

        /// <summary>
        /// Blocking receive TCP call.
        /// </summary>
        /// <returns></returns>
        public bool ProcessReceive()
        {
            try
            {                
                //OwningConnection.MyTCPSocket.Blocking = true;
                SocketDebug(5);
                if (OwningConnection.MyTCPSocket.Connected && OwningConnection.MyTCPSocket.Poll(0, SelectMode.SelectRead)) 
                {
                    SocketDebug(6);                    
                    m_TCPBytesReceived = OwningConnection.MyTCPSocket.Receive(m_TCPBuffer, 0, m_TCPBuffer.Length, SocketFlags.None, out m_LastTCPReceiveStatus);
                    SocketDebug(7);
                    Log.LogMsg("Received " + m_TCPBytesReceived.ToString() + " on socket " + OwningConnection.MyTCPSocket.Handle.ToString() + " with socket error reading " + m_LastTCPReceiveStatus.ToString());
                    
                    OnReceiveResolved(false, m_TCPBuffer, m_TCPBytesReceived, m_LastTCPReceiveStatus, m_TCPSockState);
                }
                else if(!OwningConnection.IsAlive)
                {
                    SocketDebug(8);
                    Log.LogMsg("DuplexBlockingTransit can't ProcessReceive because OwningConnection is not connected.");
                    Log.LogMsg("Can't receive on socket " + OwningConnection.MyTCPSocket.Handle.ToString() + " with socket error reading " + m_LastTCPReceiveStatus.ToString());
                    OwningConnection.KillConnection("Couldn't receive. Connection has been closed.");
                    SocketDebug(9);
                }
            }
            catch (Exception e)
            {
                string error = "";// OwningConnection.MyTCPSocket.GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.Error).ToString();
                Log.LogMsg("Error receiving data on DuplexBlockingTransit. (" + error + ")" + e.Message + ", Stack=\r\n" + e.StackTrace);
                OwningConnection.KillConnection("Connection lost! Network receive error: " + e.Message);
            }
            return true;
        }

        public void SocketDebug(int num)
        {
            /*
            string error = "";// OwningConnection.MyTCPSocket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Error).ToString();
            Log.LogMsg("[" + num.ToString() + "] SocketObjectHash=" + OwningConnection.MyTCPSocket.GetHashCode().ToString() + ", Socket.Handle=" + OwningConnection.MyTCPSocket.Handle.ToString() + ", IsAlive=" + OwningConnection.MyTCPSocket.Connected.ToString() + ", LastTCPReceiveStatus " + m_LastTCPReceiveStatus.ToString() + ", LastError=" + error);
             */
        }

        private void SendBytes(bool isUDP, byte[] data)
        {
             try
             {

                SocketDebug(10);
                if (!OwningConnection.IsConnected)
                {
                    SocketDebug(11);
                    Log.LogMsg("DuplexBlockingTransit can't send bytes. OwningConnection is not connected.");
                    return;
                }

                if (isUDP)
                {
                    SocketDebug(12);
                    OwningConnection.MyUDPSocket.SendTo(data, OwningConnection.UDPSendTarget);
                    // Log.LogMsg("Testy UDP SEND COMPLETE. Is owning connection still alive? " + OwningConnection.IsAlive);
                }
                else
                {
                    //OwningConnection.MyTCPSocket.Blocking = true;
                    SocketDebug(15);
                    OwningConnection.MyTCPSocket.Send(data);
                    SocketDebug(16);
                }

                SocketDebug(17);
                OwningConnection.SentBytes(data);
                OwningConnection.PacketSent();
                SocketDebug(18);
            }
            catch (Exception e)
            {
                Log.LogMsg("Error SendBytes. " + e.Message);
                OwningConnection.KillConnection("Send error. " + e.Message);
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
                Log.LogMsg("DuplexBlockingTransitStrategy - cant listen for data on socket. Owning connection is not connected.");
                return false;
            }

            return true;
        }       

        /// <summary>
        /// Gets called when a receive operation resolves.  If we were listening for data and the connection
        /// closed, that also counts as a receive operation resolving.
        /// </summary>
        /// <param name="args"></param>
        private void OnReceiveResolved(bool isUDP, byte[] buffer, int bytesReceived, SocketError status, SockState sockState)
        {
            SocketDebug(19);
            //// Log.LogMsg("==>++++ Async RECEIVE Op Completed - #" + ((SockState)args.UserToken).ID.ToString() + "#");
            try
            {
                if (!OwningConnection.IsAlive && bytesReceived > 0)
                {
                    SocketDebug(20);                    
                    return;
                }

                SocketDebug(21);
                // If there was a socket error, close the connection. This is NOT a normal
                // situation, if you get an error here.
                if (status != SocketError.Success)
                {
                    SocketDebug(22);
                    Log.LogMsg("Receive status = " + status.ToString());
                    if (!OwningConnection.ShuttingDown)
                    {
                        //// Log.LogMsg("Testy 111");
                        OwningConnection.KillConnection("Connection lost! Network receive error: " + status);
                    }
                    //Jump out of the ProcessReceive method.
                    SocketDebug(23);
                    return;
                }

                SocketDebug(24);
                m_TCPSockState.AsyncEventArgs.RemoteEndPoint = OwningConnection.MyTCPSocket.RemoteEndPoint;
                
                // If no data was received, close the connection. This is a NORMAL
                // situation that shows when the client has finished sending data.
                if (bytesReceived == 0)
                {
                    SocketDebug(25);
                    if (!OwningConnection.ShuttingDown)
                    {
                        //// Log.LogMsg("Testy 114");
                        OwningConnection.KillConnection("Connection closed by remote host.");
                    }
                    return;
                }

                SocketDebug(26);
                OwningConnection.ReceivedBytes(bytesReceived);

                // restart listening process    
                SocketDebug(27);
                OwningConnection.AssembleInboundPacket(buffer, bytesReceived, sockState);
                SocketDebug(28);
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
                SocketDebug(29);
                if (msg.NeedsDeliveryAck)
                {
                    m_NumAcksWaitingFor++;
                }

                SocketDebug(30);
                byte[] raw = OwningConnection.SerializePacket(msg);
                SocketDebug(31);
                Log.LogMsg("Sending -> " + msg.GetType().ToString() + " (" + raw.Length.ToString() + " bytes)");
                Log.LogMsg("That message is " + raw.Length.ToString() + " bytes long");
                Log.LogMsg("1 Socket " + OwningConnection.MyTCPSocket.Handle.ToString() + "IsAlive=" + OwningConnection.MyTCPSocket.Connected.ToString() + ", with socket error reading " + m_LastTCPReceiveStatus.ToString());
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
            SocketDebug(32);
            //Log.LogMsg("@__@ Monitor Enter");
            if (!OwningConnection.IsAlive)
            {
                Log.LogMsg("DuplexBlockingTransit - cant send data because owning connection isn't alive. MyTcpSocket == Null -> " + (OwningConnection.MyTCPSocket == null));
                Log.LogMsg("Can't send on socket " + OwningConnection.MyTCPSocket.Handle.ToString() + " with socket error reading " + m_LastTCPReceiveStatus.ToString());
                OwningConnection.KillConnection("Couldn't send. Connection has been closed.");
                return -1;
            }
            SocketDebug(33);
            // Log.LogMsg("==>Sending " + data.Length.ToString() + " bytes.");
            try
            {
                bool isUDP = (flags & PacketFlags.UDP) != 0;
                if (isUDP)
                {
                    SocketDebug(34);
                    if (!OwningConnection.CanSendUDP)
                    {
                        Log.LogMsg("!!! Tried sending UDP packet when the connection wasn't yet ready to send UDP. Dropping packet.");
                        return 0;
                    }

                    SocketDebug(35);
                    // Don't allow fragmenting UDP packets
                    if (data.Length > 1024)
                    {
                        Log.LogMsg("Message exceeded UDP size of 1024 bytes. Sending via TCP instead.");
                        flags &= ~PacketFlags.UDP;
                    }                    
                }

                SocketDebug(36);
                NetQItem qi = new NetQItem();
                qi.Flags = flags;
                qi.Data = data;
                qi.IsUDP = isUDP;
                m_SendQueue.Enqueue(qi);
                Log.LogMsg("Send queue now has " + m_SendQueue.Count.ToString() + " items in it.");
                SocketDebug(37);
                return 1;
            }
            catch (Exception sendExc)
            {
                Log.LogMsg("DuplexBlockingTransit - Failed to send data. " + sendExc.Message);
                return -1; ;
            }

            return 1;
        }

        public void InitTCP()
        {            
            //OwningConnection.MyTCPSocket.Blocking = true;
        }

        public void InitUDP()
        {        
        }

        public void BeforeShutdown()
        {            
        }

        public void AfterShutdown()
        {
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


    }
}
