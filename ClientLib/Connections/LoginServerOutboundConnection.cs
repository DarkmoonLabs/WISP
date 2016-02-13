//#define UNITY
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace Shared
{
    public class LoginServerOutboundConnection 
#if UNITY
        : UnityClientConnection
#else
        : ClientConnection
#endif
    {
        #region LoginResolved Event
        public delegate void LoginResultDelegate(LoginServerOutboundConnection loginConnection, PacketLoginResult lr);
        private LoginResultDelegate LoginResolvedInvoker;

        /// <summary>
        /// Fires when the central server connection has been severed, for any reason.
        /// </summary>
        public event LoginResultDelegate LoginResolved
        {
            add
            {
                AddHandler_LoginResolved(value);
            }
            remove
            {
                RemoveHandler_LoginResolved(value);
            }
        }

        
        private void AddHandler_LoginResolved(LoginResultDelegate value)
        {
            LoginResolvedInvoker = (LoginResultDelegate)Delegate.Combine(LoginResolvedInvoker, value);
        }

        
        private void RemoveHandler_LoginResolved(LoginResultDelegate value)
        {
            LoginResolvedInvoker = (LoginResultDelegate)Delegate.Remove(LoginResolvedInvoker, value);
        }

        private void FireLoginResolved(LoginServerOutboundConnection con, PacketLoginResult lr)
        {
            if (LoginResolvedInvoker != null)
            {
                LoginResolvedInvoker(con, lr);
            }
        }
        #endregion

        public List<GameServerInfo> GameServers = new List<GameServerInfo>();

        public LoginServerOutboundConnection(bool isBlocking)
#if UNITY
            : base()
#else
            : base(isBlocking)
#endif
        {
            RegisterPacketHandler((int)PacketType.PacketRijndaelExchangeRequest, delegate { Client.ConnectionPhase = ClientConnectionPhase.LoginServerGreeting; });
            RegisterPacketHandler((int)PacketType.LineSecured, delegate { Client.ConnectionPhase = ClientConnectionPhase.LoginServerConnectionEncrypted; });            
        }

        protected override void OnConnected(bool success, string msg)
        {
            if (success)
            {
                Client.ConnectionPhase = ClientConnectionPhase.LoginServerConnected;
            }

            base.OnConnected(success, msg);
        }

       

        protected override void OnServerLoginResponse(PacketLoginResult msg)
        {
            base.OnServerLoginResponse(msg);
            
            // At this point we're encrypted, if we want to be
            if (msg.ReplyCode != ReplyType.OK)
            {
                FireLoginResolved(this, msg);
                Log.LogMsg("Login: " + msg.ReplyCode.ToString() + ": " + msg.ReplyMessage);
                return;
            }

            Client.ConnectionPhase = ClientConnectionPhase.LoginServerLoggedIn;

            GameServers.Clear();
            string[] servers = msg.ReplyMessage.Trim('|').Split('|');
            for (int i = 0; i < servers.Length; i++)
            {
                string[] serverData = servers[i].Split(',');
                if (serverData.Length != 2)
                {
                    KillConnection("Login server sent malformed game server data.  Are you running the correct version of the client?");
                    return;
                }

                try
                {
                    for (int x = 0; x < serverData.Length; x += 2)
                    {
                        GameServerInfo gsi = new GameServerInfo();
                        gsi.Name = serverData[x];
                        //gsi.IP = serverData[x + 1];
                        //gsi.Port = int.Parse(serverData[x + 2]);
                        //gsi.CurUsers = int.Parse(serverData[x + 3]);
                        //gsi.MaxUsers = int.Parse(serverData[x + 4]);
                        gsi.IsOnline = bool.Parse(serverData[x + 1]);
                        GameServers.Add(gsi);
                    }

                    Client.ConnectionPhase = ClientConnectionPhase.LoginServerGotWorldServerListing;
                }
                catch (FormatException)
                {
                    KillConnection("Login server sent malformed game server data.  Are you running the correct version of the client?");
                    return;
                }

                FireLoginResolved(this, msg);
            }
        }

        public void RequestHandoffToServer(GameServerInfo gsi, Guid targetResource)
        {            
            PacketRequestHandoffToServerCluster ho = (PacketRequestHandoffToServerCluster)CreatePacket((int)PacketType.RequestHandoffToServer, 0, true, true);
            ho.TargetServerName = gsi.Name;
            ho.TargetResource = targetResource;
            ho.NeedsReply = true;
            Client.ConnectionPhase = ClientConnectionPhase.LoginServerRequestedClusterServerAccess;
            Send(ho);
        }        

    }
}
