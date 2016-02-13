using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace Shared
{
    /// <summary>
    /// Represents one player connection coming into the lobby hive login server.
    /// </summary>
    public class InboundLobbyLoginConnection : LSInboundConnection
    {
        public InboundLobbyLoginConnection(Socket s, ServerBase server, bool isBlocking) : base (s, server, isBlocking)
        {
        }

        private bool m_IsNewAcct = false;

        protected override bool CanCreateNewAccount(Packet p, ref string msg)
        {
            m_IsNewAcct = true;
            return base.CanCreateNewAccount(p, ref msg);
        }

        protected override bool HaveServersForService()
        {
            GameServerInfo<OutboundServerConnection> cons = GetTargetServerForClusterHandoff("");
            return cons != null;
        }

        protected override PacketLoginResult CreateLoginResultPacket()
        {
            PacketLoginResult lr = (PacketLoginResult)CreatePacket((int)PacketType.LoginResult, 0, true, true);
            lr.ReplyCode = ReplyType.OK;

            GameServerInfo<OutboundServerConnection> cons = GetTargetServerForClusterHandoff("");
            bool haveAnyOnline = cons != null;
            if (cons != null)
            {
                lr.ReplyMessage = cons.Name + "," + "TRUE|";
            }

            if (!haveAnyOnline)
            {
                lr.ReplyMessage = "No servers available for service. Try again later.";
                lr.IsCritical = true;
                lr.ReplyCode = ReplyType.Failure;
            }

            Log1.Logger("LoginServer.Inbound.Login").Debug("Sending client " + ServerUser.AccountName + " game server info: " + lr.ReplyMessage);

            if (m_IsNewAcct)
            {
                ServerUser.Profile.Save(MyServer.RequireAuthentication);

                if (lr.ReplyCode != ReplyType.OK)
                {
                    lr.ReplyMessage += "\r\n(Account was created, however)";
                }
            }
           
            return lr;
        }

        protected override GameServerInfo<OutboundServerConnection> GetServerInfo(string serverName)
        {
            GameServerInfo<OutboundServerConnection> gsi = null;

            string serverType = "";
            string address = "";
            int port = -1;
            int curConnections = -1;
            int maxConnections = -1;

            // Grab all registered content servers.
            if (DB.Instance.Server_GetRegistrations(serverName, out serverType, out address, out port, out  curConnections, out maxConnections))
            {
                gsi = new GameServerInfo<OutboundServerConnection>();
                gsi.Name = serverName;
                gsi.IP = address;
                gsi.Port = port;
                gsi.CurUsers = curConnections;
                gsi.MaxUsers = maxConnections;

                if (gsi.CurUsers >= gsi.MaxUsers)
                {
                    return null;
                }
            }

            return gsi;
        }

        protected override GameServerInfo<OutboundServerConnection> GetTargetServerForClusterHandoff(string serverGroup)
        {
            GameServerInfo<OutboundServerConnection> gsi = null;

            List<string> addresses;
            List<string> ids;
            List<int> ports;
            List<int> curConnections;
            List<int> maxConnections;

            // Grab all registered content servers.
            if (DB.Instance.Server_GetRegistrations("", "lobby", out addresses, out ids, out ports, out curConnections, out maxConnections))
            {
                // find the lowest population server
                int low = 0;
                float lowRatio = 1f;
                for (int i = 0; i < ids.Count; i++)
                {
                    float ratio = (float)curConnections[i] / (float)maxConnections[i];
                    if (ratio < lowRatio)
                    {
                        lowRatio = ratio;
                        low = i;
                    }
                }

                if (lowRatio >= 1)
                {
                    // All servers are at capacity.
                    return null;
                }

                // Create a temp object with latest info from DB
                gsi = new GameServerInfo<OutboundServerConnection>();
                gsi.Name = ids[low];
                gsi.IP = addresses[low];
                gsi.Port = ports[low];
                gsi.CurUsers = curConnections[low];
                gsi.MaxUsers = maxConnections[low];
            }

            return gsi;
        }
    }
}
