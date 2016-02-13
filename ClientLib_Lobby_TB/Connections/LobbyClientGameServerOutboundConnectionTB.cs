using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared
{
    public class LobbyClientGameServerOutboundConnectionTB : LobbyClientGameServerOutboundConnection
    {
        public LobbyClientGameServerOutboundConnectionTB(bool isBlocking)
            : base(isBlocking)
        {
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            NetworkConnection.RegisterPacketCreationDelegate((int)LobbyPacketType.GameMessage, (byte)TurnedGameMessageSubType.TurnOrderUpdate, delegate { return new PacketTurnOrderUpdate(); });
            NetworkConnection.RegisterPacketCreationDelegate((int)LobbyPacketType.GameMessage, (byte)TurnedGameMessageSubType.PhaseUpdate, delegate { return new PacketPhaseUpdate(); });
            NetworkConnection.RegisterPacketCreationDelegate((int)LobbyPacketType.GameMessage, (byte)TurnedGameMessageSubType.PlayerDone, delegate { return new PacketGameMessage(); });
        }
        
    }
}
