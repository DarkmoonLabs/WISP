using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Reflection;
using System.Net;
using Shared;

namespace ServerLib
{
    /// <summary>
    /// Listens for connections on port 843 and dispatches requests to a PolicyConnection.  Partial trust Unity Web application
    /// can not connect to any remote machines unless those machines run a policy server, such as this one.  The policy server
    /// must run on Port 843 in order for default Unity Web applications configurations to find it.
    /// <para>Under normal circumstances, this class does not need to be instantiated as it's automatically handled by ServerBase</para>
    /// </summary>
    public class UnityWebPolicyServer
    {
        private Socket m_listener;
        private byte[] m_policy;

        // pass in the path of an XML file containing the socket policy
        public UnityWebPolicyServer(string policyFile)
        {
            try
            {
                // Load the policy file
                m_policy = File.ReadAllBytes(policyFile);

                // Create the Listening Socket
                m_listener = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);

                // Put the socket into dual mode to allow a single socket 
                // to accept both IPv4 and IPv6 connections
                // Otherwise, server needs to listen on two sockets,
                // one for IPv4 and one for IPv6
                // NOTE: dual-mode sockets are supported on Vista and later
                m_listener.SetSocketOption(SocketOptionLevel.IPv6, (SocketOptionName)27, 0);

                m_listener.Bind(new IPEndPoint(IPAddress.IPv6Any, 843));
                m_listener.Listen(25);
                m_listener.BeginAccept(new AsyncCallback(OnConnection), null);
                Log1.Logger("Network").Info("Unity3d Web Policy Server running on port 843.");
            }
            catch (Exception e)
            {
                Log1.Logger("Server.Network").Error("Error starting Unity3d Web Policy server on this process. " + e.Message, e);
            }
        }

        // Called when we receive a connection from a client
        public void OnConnection(IAsyncResult res)
        {
            Socket client = null;

            try
            {
                client = m_listener.EndAccept(res);
            }
            catch (SocketException)
            {
                return;
            }

            Log1.Logger("Network").Info("Unity3d Web connection from " + client.RemoteEndPoint.ToString());
            // handle this policy request with a PolicyConnection
            PolicyConnection pc = new PolicyConnection(client, m_policy);

            // look for more connections
            m_listener.BeginAccept(new AsyncCallback(OnConnection), null);
        }

        public void Close()
        {
            m_listener.Close();
        }
    }

}
