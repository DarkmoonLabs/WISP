using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Net;

namespace Shared
{
    /// <summary>
    /// Represents one connection, initiated by the central server to one of the content sub-servers 
    /// </summary>
    public class CentralLobbyOutboundContentConnection : CSOutboundConnection
    {
        public CentralLobbyOutboundContentConnection(string name, ServerBase server, string reportedIP, bool isBlocking)
            : base(name, server, reportedIP, isBlocking)
        {            
        }

        protected override void OnSocketKilled(string msg)
        {
            base.OnSocketKilled(msg);
            Log.LogMsg("Lost connection to " + Name + " (" + msg + ") Assuming server is unavailable. Delisting all content for that server.");
            if (ServerUserID != null && ServerUserID.Length > 0)
            {
                // Clean the DB game data which is read by other lobby server centrals.
                DB.Instance.Lobby_UntrackGamesForServer(ServerUserID);
                
                // Take that content server out of the transfer target table.  we're assuming the server is inaccesible.
                DB.Instance.Server_Unregister(ServerUserID);
            }
        }
       
    }
}
