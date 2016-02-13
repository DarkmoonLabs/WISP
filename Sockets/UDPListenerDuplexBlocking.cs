#if !SILVERLIGHT

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Net.Sockets;
    using System.Net;

    namespace Shared
    {
        public class UDPListenerDuplexBlocking : IUDPListener
        {
            /// <summary>
            /// The UDP socket with which we listen to all incoming UDP traffic on a given port.
            /// </summary>
            public Socket Socket { get; set; }
            private byte[] m_ListenArgs;
            private Func<IPEndPoint, INetworkConnection> GetConnnection;
            private INetworkConnection m_Owner = null;
            public bool Listening { get; set; }
            public int Port { get; set; }
            private SockState m_State;
            private EndPoint m_EndPoint;

            public void StopNetworkingPump()
            {
            }

            public void ProcessReceive()
            {
                try
                {
                    if (Socket == null)
                    {
                        Log.LogMsg("SOCKET is null.  Can't process UDP receive.");
                        return;
                    }

                    Socket.Blocking = true;
                    while (Socket.Poll(0, SelectMode.SelectRead))
                    {
                        Log.LogMsg(this.GetType().ToString() + " reading on socket " + Socket.Handle.ToString());
                        int numGot = Socket.ReceiveFrom(m_ListenArgs, ref m_EndPoint);
                        Log.LogMsg("Received " + numGot.ToString() + " UDP bytes.");
                        OnReceiveResolved(numGot, m_ListenArgs, m_EndPoint);
                    }
                }
                catch (SocketException se)
                {
                   Log.LogMsg("Error receiving data on UDP DuplexBlockingTransit. " + se.Message + ", Stack=\r\n" + se.StackTrace);
                   m_Owner.KillConnection("Connection lost! Network receive error: " + se.Message);
                }
                catch(Exception e)
                {
                    m_Owner.KillConnection("Connection lost! Network receive error: " + e.Message);
                }
            }

            public bool StartListening(System.Net.Sockets.AddressFamily family, int port, int maxSimultaneousListens, Func<System.Net.IPEndPoint, INetworkConnection> getConMethod)
            {
                return StartListening(family, port, getConMethod);
            }

            public bool StartListening(AddressFamily family, int port, Func<IPEndPoint, INetworkConnection> getConMethod)
            {
                try
                {
                    GetConnnection = getConMethod;
                    if (Socket == null)
                    {
                        Socket = new System.Net.Sockets.Socket(family, SocketType.Dgram, ProtocolType.Udp);
                        Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                        Socket.ExclusiveAddressUse = false;
                        if (family == AddressFamily.InterNetworkV6)
                        {
                            Socket.SetSocketOption(SocketOptionLevel.IPv6, (SocketOptionName)27, 0); // set IPV6Only to false.  enables dual mode socket - V4+v6
                            Socket.Bind(new IPEndPoint(IPAddress.IPv6Any, port));
                        }
                        else
                        {
                            Socket.Bind(new IPEndPoint(IPAddress.Any, port));
                        }
                    }
                    Port = ((IPEndPoint)Socket.LocalEndPoint).Port;
                    m_State = new SockState(null, 1024, null);
                       
                    if (family == AddressFamily.InterNetworkV6)
                    {
                        m_EndPoint = new IPEndPoint(IPAddress.IPv6Any, 0);
                    }
                    else
                    {
                        m_EndPoint = new IPEndPoint(IPAddress.Any, 0);
                    }

                }
                catch (Exception e)
                {
                    Log.LogMsg("UDPListenerDuplexBlocking - error start listen: " + e.Message);
                    return false;
                }
                Listening = true;
                Log.LogMsg("UDPListenerDuplexBlocking - Listening for UDP traffic on port " + port.ToString());
                return true;
            }
            
            public void StopListening()
            {
                try
                {
                    if (Socket != null)
                    {
                        Log.LogMsg("Shutting down UDP listener duplex blocking Socket.");
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
                        Log.LogMsg("Closing UDP listener duplex blocking Socket.");
                        Socket.Close();
                        Socket = null;
                    }
                    Listening = false;
                }
            }

            public bool StartListening(AddressFamily family, int port, int maxSimultaneousListens, INetworkConnection owner)
            {
                m_Owner = owner;
                return StartListening(family, port, ipe => { return m_Owner; });
            }

            /// <summary>
            /// Gets called when a receive operation resolves.  If we were listening for data and the connection
            /// closed, that also counts as a receive operation resolving.
            /// </summary>
            /// <param name="args"></param>
            private void OnReceiveResolved(int numGot, byte[] data, EndPoint ep)
            {
                //// Log.LogMsg("==>++++ Async RECEIVE Op Completed - #" + ((SockState)args.UserToken).ID.ToString() + "#");
                INetworkConnection con = null;
                try
                {
                    if (!Listening)
                    {
                        return;
                    }

                    con = GetConnnection(ep as IPEndPoint);

                    // If no data was received, close the connection. This is a NORMAL
                    // situation that shows when the client has finished sending data.
                    if (numGot == 0)
                    {
                        con.KillConnection("Connection closed by remote host.");
                        return;
                    }

                    // restart listening process                
                    con.AssembleInboundPacket(data, numGot, m_State);
                }
                catch (Exception ex)
                {
                    Log.LogMsg("UDPListenerDuplexBlocking Error ProcessReceive. " + ex.Message);
                    if (con != null)
                    {
                        con.KillConnection("UDPListenerDuplexBlocking Error receive. " + ex.Message);
                    }
                }


            }

        }
    }
#endif
