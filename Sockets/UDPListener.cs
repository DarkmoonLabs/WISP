#if !SILVERLIGHT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;

namespace Shared
{
    public class UDPListener : IUDPListener
    {
        /// <summary>
        /// The UDP socket with which we listen to all incoming UDP traffic on a given port.
        /// </summary>
        public Socket Socket { get; set; }

        private List<SocketAsyncEventArgs> m_ListenArgs;
        private Func<IPEndPoint, INetworkConnection> GetConnnection;
        private INetworkConnection m_Owner = null;
        public bool Listening { get; set; }
        private bool m_ShuttingDown = false;
        public int Port { get; set; }

        public bool StartListening(AddressFamily family, int port, int maxSimultaneousListens, Func<IPEndPoint, INetworkConnection> getConMethod )
        {
            try
            {
                GetConnnection = getConMethod;
                if (Socket == null)
                {
                    //// Log.LogMsg("Testy 30");
                    Socket = new System.Net.Sockets.Socket(family, SocketType.Dgram, ProtocolType.Udp);
                    //// Log.LogMsg("Testy 31");
                    Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    //// Log.LogMsg("Testy 32");
                    Socket.ExclusiveAddressUse = false;
                    //// Log.LogMsg("Testy 33");
                    if (family == AddressFamily.InterNetworkV6)
                    {
                        //// Log.LogMsg("Testy 34");
                        Socket.SetSocketOption(SocketOptionLevel.IPv6, (SocketOptionName)27, 0); // set IPV6Only to false.  enables dual mode socket - V4+v6
                        //// Log.LogMsg("Testy 35");
                        Socket.Bind(new IPEndPoint(IPAddress.IPv6Any, port));
                        //// Log.LogMsg("Testy 36");
                    }
                    else
                    {
                        //// Log.LogMsg("Testy 37");
                        Socket.Bind(new IPEndPoint(IPAddress.Any, port));
                    }
                }
                //// Log.LogMsg("Testy 38");
                Port = ((IPEndPoint)Socket.LocalEndPoint).Port;

                m_ListenArgs = new List<SocketAsyncEventArgs>(maxSimultaneousListens);
                for (int i = 0; i < maxSimultaneousListens; i++)
                {
                    //// Log.LogMsg("Testy 39");
                    SocketAsyncEventArgs arg = SocketAsyncEventArgsCache.PopReadEventArg(new EventHandler<SocketAsyncEventArgs>(ReadCompleted), Socket);
                    if (family == AddressFamily.InterNetworkV6)
                    {
                        arg.RemoteEndPoint = new IPEndPoint(IPAddress.IPv6Any, 0);
                    }
                    else
                    {
                        arg.RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    }

                    //// Log.LogMsg("Testy 40");
                    m_ListenArgs.Add(arg);
                    //// Log.LogMsg("Testy 41");
                    bool willRaiseEvent = Socket.ReceiveFromAsync(arg);
                    //// Log.LogMsg("Testy 999");
                    if (!willRaiseEvent)
                    {
                        //// Log.LogMsg("Testy 42");
                        OnReceiveResolved(arg);
                    }
                }
            }
            catch(Exception e)
            {
                Log.LogMsg("UDPListener - error start listen: " + e.Message);
                return false;
            }
            Listening = true;
            Log.LogMsg("UDPListener - Listening for UDP traffic on port " + port.ToString() + " with " + maxSimultaneousListens.ToString() + " max listeners.");
            return true;
        }

        public void StopListening()
        {            
            try
            {
                m_ShuttingDown = true;                

                if (Socket != null)
                {
                    Log.LogMsg("Shutting down UDP listener Socket. ");
                    StackTrace stackTrace = new StackTrace();           // get call stack
                    StackFrame[] stackFrames = stackTrace.GetFrames();  // get method calls (frames)

                    // write call stack method names
                    foreach (StackFrame stackFrame in stackFrames)
                    {
                        Log.LogMsg(stackFrame.GetFileName() + ", " + stackFrame.GetFileLineNumber() + " :" + stackFrame.GetMethod().Name);   // write method name
                    }

                    Socket.Shutdown(SocketShutdown.Both);             
                }
            }
            catch (Exception e)
            {
            }
            finally
            {
                if (Socket != null)
                {
                    Log.LogMsg("Closing UDP listener Socket.");
                    Socket.Close();                    
                    Socket = null;
                }
                if (m_ListenArgs != null)
                {
                    for (int i = 0; i < m_ListenArgs.Count; i++)
                    {
                        SocketAsyncEventArgsCache.PushReadEventArg(m_ListenArgs[i], new EventHandler<SocketAsyncEventArgs>(ReadCompleted));
                    }
                }
                Listening = false;
            }
        }

        public bool StartListening(AddressFamily family, int port, int maxSimultaneousListens, INetworkConnection owner)
        {
            m_Owner = owner;
            return StartListening(family, port, maxSimultaneousListens, ipe => { return m_Owner; });
        }

        private void ReadCompleted(object sender, SocketAsyncEventArgs e)
        {
            try
            {
                if (e.LastOperation != SocketAsyncOperation.ReceiveFrom)
                {
                    throw new ArgumentException("The last operation completed on the socket was not a receive or send");
                }
                OnReceiveResolved(e);

            }
            catch (Exception ex)
            {
                Log.LogMsg("Error in UDP Listener ReadCompleted. " + ex.Message);
            }
        }

        /// <summary>
        /// Gets called when a receive operation resolves.  If we were listening for data and the connection
        /// closed, that also counts as a receive operation resolving.
        /// </summary>
        /// <param name="args"></param>
        private void OnReceiveResolved(SocketAsyncEventArgs args)
        {
            //// Log.LogMsg("Testy 50");
            //// Log.LogMsg("==>++++ Async RECEIVE Op Completed - #" + ((SockState)args.UserToken).ID.ToString() + "#");
            INetworkConnection con = null;
            try
            {
                if (m_ShuttingDown || !Listening)
                {
                    return;
                }

                //// Log.LogMsg("Testy 51");
                con = GetConnnection(args.RemoteEndPoint as IPEndPoint);
                if (args.SocketError == SocketError.Success && con == null || !con.IsAlive)
                {
                    //// Log.LogMsg("Testy 52");
                    throw new ArgumentException("UDPListener - received packet from unknown connection " + args.RemoteEndPoint.ToString());
                }

                SockState state = args.UserToken as SockState;
                if (args.SocketError != SocketError.Success)
                {
                    //// Log.LogMsg("Testy 53");
                    con.KillConnection("UDP Connection lost! Network receive error: " + args.SocketError.ToString());
                    //Jump out of the ProcessReceive method.
                    return;
                }

                // If no data was received, close the connection. This is a NORMAL
                // situation that shows when the client has finished sending data.
                if (args.BytesTransferred == 0)
                {
                    //// Log.LogMsg("Testy 54");
                    con.KillConnection("Connection closed by remote host.");
                    return;
                }

                // restart listening process  
                //// Log.LogMsg("Testy 55");
                con.AssembleInboundPacket(args, state);               
            }
            catch (Exception ex)
            {
                Log.LogMsg("UDPListener Error ProcessReceive. " + ex.Message);
                if (con != null)
                {
                    con.KillConnection("UDPListener Error receive. " + ex.Message);
                }
            }
            finally
            {
                if (!m_ShuttingDown)
                {
                    bool willRaiseEvent = false;

                    // Post async receive operation on the socket.
                    //Log.LogMsg("==><<<< TCP Async RECEIVE op started - #" + ((SockState)m_RecArgs.UserToken).ID.ToString() + "#");
                    willRaiseEvent = Socket.ReceiveFromAsync(args);
                    if (!willRaiseEvent)
                    {
                        OnReceiveResolved(args);
                    }
                }
            }



        }
       
    }
}
#endif