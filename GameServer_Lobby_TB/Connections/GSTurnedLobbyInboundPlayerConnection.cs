using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace Shared
{
    /// <summary>
    /// Represents the connection between the player and the lobby content server
    /// </summary>
    public class GSTurnedLobbyInboundPlayerConnection : GSLobbyInboundPlayerConnection
    {
        public GSTurnedLobbyInboundPlayerConnection(Socket s, ServerBase server, bool isBlocking)
            : base(s, server, isBlocking)
        {
        }

        protected override GameServerGame OnCreateNewGameServerGame(Game game)
        {
            TurnedGameServerGame g = new TurnedGameServerGame(game);
            game.Decorator = g;
            return g;            
        }

        private static bool m_IsInitialized = false;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            if (!m_IsInitialized)
            {                
                m_IsInitialized = true;
                RegisterPacketCreationDelegate((int)TurnedGameMessageSubType.TurnOrderUpdate, delegate { return new PacketTurnOrderUpdate(); });
                RegisterPacketCreationDelegate((int)LobbyPacketType.GameMessage, (int)TurnedGameMessageSubType.PlayerDone, delegate { return new PacketGameMessage(); });
            }
        }     

    }

}
