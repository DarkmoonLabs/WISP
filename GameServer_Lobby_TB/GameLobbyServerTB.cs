using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace Shared
{
    /// <summary>
    /// A lobby/instance/game/match content server that handles turn based game logic.
    /// </summary>
    public class GameLobbyServerTB : GameLobbyServer
    {
        public override void StartServer()
        {
            base.StartServer();

            // We will transfer these types across the wire, so we must register them so that they can be reconsituted
            Factory.Instance.Register(typeof(BeginningOfTurnPhase), delegate {return new BeginningOfTurnPhase(0);});
            Factory.Instance.Register(typeof(EndOfTurnPhase), delegate { return new EndOfTurnPhase(0); });
            Factory.Instance.Register(typeof(MainPhase), delegate { return new MainPhase(0); });
            Factory.Instance.Register(typeof(RoundEndSequenceItem), delegate { return new RoundEndSequenceItem(0); });
            Factory.Instance.Register(typeof(RoundStartupSequenceItem), delegate { return new RoundStartupSequenceItem(0); });
            
#if DEBUG
            //Test();            
#endif
        }

        /// <summary>
        /// Override from ServerBase, to make sure we create the proper connection object for inbound connections.
        /// If we don't override this method, a generic InboundConnection class will be instantiated.
        /// </summary>
        protected override InboundConnection CreateInboundConnection(Socket s, ServerBase server, int serviceID, bool isBlocking)
        {
            InboundConnection con = null;
            // ServiceIDs represents the type connection that is being requested.
            // these IDs are set in the App.Config of the initiating server and are any arbitrarily agreed upon integers
            switch (serviceID)
            {
                case 7:
                    con = new ZeusInboundConnection(s, server, isBlocking);
                    con.ServiceID = serviceID;
                    break;
                case 1: // central server
                    con = new GSTurnedLobbyInboundCentralConnection(s, server, isBlocking);
                    con.ServiceID = serviceID;
                    GameManager.Instance.CentralServer = con as GSInboundServerConnection;
                    break;
                default: // assume player client
                    con = new GSTurnedLobbyInboundPlayerConnection(s, server, isBlocking);
                    con.ServiceID = serviceID;
                    break;
            }

#if DEBUG
            if (con == null)
            {
                throw new ArgumentOutOfRangeException("ServiceID " + serviceID.ToString() + " is unknown to CreateInboundConnection.  Cannot process connection.");
            }
#endif
            return con;
        }

        private void Test()
        {
            Game g = new Game();
            PropertyBag p = new PropertyBag();
            g.Properties = p;
            g.Owner = -1;

            TurnedGameServerGame tg = new TurnedGameServerGame(g);
            string msg ="";
            CharacterInfo ci1 = new CharacterInfo();
            ci1.ID = 1;
            ci1.CharacterName = "Alpha";
            ServerCharacterInfo t1 = new ServerCharacterInfo(ci1);
            tg.AddPlayer(t1, ref msg);

            CharacterInfo ci2 = new CharacterInfo();
            ci2.ID = 2;
            ci2.CharacterName = "Bravo";
            ServerCharacterInfo t2 = new ServerCharacterInfo(ci2);
            tg.AddPlayer(t2, ref msg);

            CharacterInfo ci3 = new CharacterInfo();
            ci3.ID = 3;
            ci3.CharacterName = "Charly";
            ServerCharacterInfo t3 = new ServerCharacterInfo(ci3);
            tg.AddPlayer(t3, ref msg);

            string msg2 ="";
            tg.StartGame(ref msg2, true);
        }
    }
}
