using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace Shared
{
    /// <summary>
    /// Encapsulate pertinent data about a remote server
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class GameServerInfo<T> where T : OutboundServerConnection 
    {
        /// <summary>
        /// If this server is part of a logical server group, this property holds that group's name
        /// </summary>
        public string ServerGroup { get; set; }

        /// <summary>
        /// The server's name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The server's resolved IP address
        /// </summary>
        public string IP { get; set; }

        /// <summary>
        /// The maximum number of user that this server will accept, as reported to us by the gate keeper (login server, most likely)
        /// </summary>
        public int MaxUsers { get; set; }

        /// <summary>
        /// The current number of user connected to the server, as reported to us by the gate keeper (login server, most likely)
        /// </summary>
        public int CurUsers { get; set; }

        /// <summary>
        /// The last time we got a ping response from this server
        /// </summary>
        public DateTime LastUpdate { get; set; }

        /// <summary>
        /// The port on which we connect to this server
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// The connection object that is used to communicate with this server
        /// </summary>
        public T Connection { get; set; }        

        /// <summary>
        /// Friendly, unresolved hostname of this server, if any
        /// </summary>
        public string HostName { get; set; }

        /// <summary>
        /// The service ID, indicating what kind of service we are to request from this server.
        /// </summary>
        public int ServiceID { get; set; }

        private bool m_IsOnline;
        /// <summary>
        /// True, if we know the server to be online, based on ping responses
        /// </summary>
        public bool IsOnline
        {
            get { return m_IsOnline; }
            set { m_IsOnline = value; }
        }

        /// <summary>
        /// The internal user ID of the server
        /// </summary>
        public string UserID { get; set; }
        
        /// <summary>
        /// A Unique ID of the server, suitable for dictionary keys, etc
        /// </summary>
        public string UniqueID
        {
            get
            {
                return GetUniqueID(IP, Port);
            }
        }

        public IPEndPoint RemoteEndPoint
        {
            get
            {
                if (Connection == null)
                {
                    return null;
                }
                return Connection.RemoteEndPoint as IPEndPoint;
            }
        }

        /// <summary>
        /// "Password" used to identify ourselves to other servers in the Hive
        /// </summary>
        public string SharedHiveKey { get; set; }

        /// <summary>
        /// Generates a unique server ID, based on the remote endpoint information
        /// </summary>
        /// <param name="ip">the IP address of the server</param>
        /// <param name="port">the port of the server</param>
        /// <returns></returns>
        public static string GetUniqueID(string ip, int port)
        {
            return ip + ":" + port;
        }       
    }
}
