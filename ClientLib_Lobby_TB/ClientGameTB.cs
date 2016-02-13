using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared
{
    public class ClientGameTB : ClientGame
    {
        public ClientGameTB(Game g, LobbyClient client, ClientGameServerOutboundConnection con) : base(g, client, con)
        {
            RegisterGamePacketHandler((int)TurnedGameMessageSubType.PhaseUpdate, OnPhaseEntered);
            RegisterGamePacketHandler((int)TurnedGameMessageSubType.TurnOrderUpdate, OnTurnOrderUpdateReceived);
        }

        /// <summary>
        /// Signal to the server that we are done and are relinquishing control.
        /// </summary>
        public void PlayerDone()
        {
            if (!Client.GameServerReadyForPlay)
            {
                Log.LogMsg("Tried to send 'Player Done', but game server not ready.");
                return;
            }

            PacketGameMessage done = new PacketGameMessage();
            done.PacketSubTypeID = (int)TurnedGameMessageSubType.PlayerDone;
            Connection.Send(done);
        }

        protected virtual void OnPhaseEntered(INetworkConnection sender, Packet p)
        {
            PacketPhaseUpdate msg = p as PacketPhaseUpdate;
            if (msg.PhaseUpdateKind == PacketPhaseUpdate.UpdateKind.EnteredWithDelay)
            {
                DateTime when = new DateTime(msg.Phase.ResponseTime, DateTimeKind.Utc);
                TimeSpan len = when - DateTime.UtcNow;
                Log.LogMsg("Phase [" + msg.Phase.PhaseName + "] will execute in [" + len.TotalSeconds + " seconds]");
                m_PendingPhase = msg.Phase;
            }
            else
            {
                Log.LogMsg("Phase [" + msg.Phase.PhaseName + "] update: [" + msg.PhaseUpdateKind.ToString() + "]");
                m_CurPhase = msg.Phase;
            }

            if (msg.Phase.PhaseID == (int)PhaseId.RoundStartup)
            {
                m_RoundNumber++;
            }

            FirePhaseEntered(this, msg.Phase, msg.PhaseUpdateKind);
        }

        private Phase m_PendingPhase;
        /// <summary>
        /// Pending phase, if there's is phase enter delay. if there is no delay, the pending phase is the same as the current phase.
        /// </summary>
        public Phase PendingPhase
        {
            get { return m_PendingPhase; }
            set { m_PendingPhase = value; }
        }
        
        private Phase m_CurPhase;
        /// <summary>
        /// Current Phase
        /// </summary>
        public Phase CurPhase
        {
            get { return m_CurPhase; }
        }
        
        private void OnTurnOrderUpdateReceived(INetworkConnection sender, Packet p)
        {
            try
            {
                PacketTurnOrderUpdate msg = p as PacketTurnOrderUpdate;
                PlayerTurnOrder = msg.CharacterIdOrder;
            }
            catch { }
        }

        public ICharacterInfo CurrentPlayer
        {
            get
            {
                int id = Properties.GetIntProperty("CurrentPlayer").GetValueOrDefault(-1);
                if (id > 0)
                {
                    ICharacterInfo ci = null;
                    if (Players.TryGetValue(id, out ci))
                    {
                        return ci;
                    }
                }

                CharacterInfo shell = new CharacterInfo(-1);
                shell.CharacterName = "";
                return shell;
            }
        }

        public List<int> PlayerTurnOrder
        {
            get
            {
                return m_PlayerTurnOrder;
            }
            set
            {
                m_PlayerTurnOrder = value;
                string order = "";
                m_PlayerTurnOrder.ForEach(id => order += id.ToString() + "|");
                Log.LogMsg("Update turn order set. [" + order + "]");
            }
        }
        private List<int> m_PlayerTurnOrder = new List<int>();

        private int m_RoundNumber = 0;
        public int RoundNumber
        {
            get
            {
                return m_RoundNumber;
            }
        }

        #region PhaseEntered Event
        private Action<Game, IGameSequencerItem, PacketPhaseUpdate.UpdateKind> PhasePhaseEnteredInvoker;

        /// <summary>
        /// Fires when the phase of the game changes. Includes "Rounds", "Turns" and sub phases
        /// </summary>
        public event Action<Game, IGameSequencerItem, PacketPhaseUpdate.UpdateKind> PhaseEntered
        {
            add
            {
                AddHandler_PhaseEntered(value);
            }
            remove
            {
                RemoveHandler_PhaseEntered(value);
            }
        }

        private void AddHandler_PhaseEntered(Action<Game, IGameSequencerItem, PacketPhaseUpdate.UpdateKind> value)
        {
            PhasePhaseEnteredInvoker = (Action<Game, IGameSequencerItem, PacketPhaseUpdate.UpdateKind>)Delegate.Combine(PhasePhaseEnteredInvoker, value);
        }

        private void RemoveHandler_PhaseEntered(Action<Game, IGameSequencerItem, PacketPhaseUpdate.UpdateKind> value)
        {
            PhasePhaseEnteredInvoker = (Action<Game, IGameSequencerItem, PacketPhaseUpdate.UpdateKind>)Delegate.Remove(PhasePhaseEnteredInvoker, value);
        }

        private void FirePhaseEntered(Game game, IGameSequencerItem item, PacketPhaseUpdate.UpdateKind kind)
        {
            if (PhasePhaseEnteredInvoker != null)
            {
                PhasePhaseEnteredInvoker(game, item, kind);
            }
        }
        #endregion

    }
}
