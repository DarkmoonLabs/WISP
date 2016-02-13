using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace Shared
{
    public class InboundChatLoginConnection : LSInboundConnection
    {
        public InboundChatLoginConnection(Socket s, ServerBase server, bool isBlocking)
            : base(s, server, isBlocking)
        {
        }
        private bool m_IsNewAcct = false;
        private string m_Alias = "";
        
        protected override bool CanCreateNewAccount(Packet p, ref string msg)
        {
            m_IsNewAcct = true;
            bool rslt = base.CanCreateNewAccount(p, ref msg);
            if (rslt)
            {
                m_Alias = ((PacketLoginRequest)p).Parms.GetStringProperty("Alias");
                if (m_Alias == null)
                {
                    msg = "Alias can't be empty.";
                    return false;
                }

                if (m_Alias.Length < 3)
                {
                    msg = "Alias must be at least 3 characters long.";
                    return false;
                }

                if (DB.Instance.Chat_DoesAliasExist(m_Alias))
                {
                    msg = "Alias already exists. Try something else.";
                    return false;
                }
            }

            return rslt;
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
                ServerUser.Profile.Alias = m_Alias;
                ServerUser.Profile.Save(MyServer.RequireAuthentication);

                if (lr.ReplyCode != ReplyType.OK)
                {
                    lr.ReplyMessage += "\r\n(Account was created, however)";
                }
            }
            lr.Parms.SetProperty("Alias", ServerUser.Profile.Alias);
            lr.Parms.SetProperty("ProfilePic", ServerUser.Profile.AddedProperties.GetByteArrayProperty("ProfilePic"));
            return lr;
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
            if (DB.Instance.Server_GetRegistrations("", "chat", out addresses, out ids, out ports, out curConnections, out maxConnections))
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
                gsi.UserID = ids[low];
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
