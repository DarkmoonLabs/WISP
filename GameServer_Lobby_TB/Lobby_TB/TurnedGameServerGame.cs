using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared
{
    /// <summary>
    /// Represents one Content server game/match/instance that supports turn based games.  Inherit from this class to
    /// create your own turn based games.
    /// </summary>
    public class TurnedGameServerGame : GameServerGame, IDisposable, ITurnedGame
    {
        /* Sequence of events:
         
            Start Game
            OnBeforeGameStart
            OnGameStarted
         * 
	            OnBeforeRoundStart
	            OnRoundStarted
		            OnBeforeNextTurn
		            OnNextTurn
			            OnBeforePhaseStart
			            OnPhaseStart 
			            OnBeforePhaseEnd
			            OnPhaseEnd
			            OnBeforePhaseStart...
		            OnBeforeTurnEnd
		            OnTurnEnded
		            OnBeforeNextTurn...
	            OnBeforeRoundEnd
	            OnRoundEnded
	            OnBeforeRoundStart...
         * 
            OnBeforeGameEnd
            OnGameEnded        
         
         */
        public TurnedGameServerGame(Game g)
            : base(g)
        {
            m_GamePhaseSequencer = new GameSequencer(this);
            m_GamePhaseSequencer.ItemExecuted += new Action<IGameSequencerItem, bool, string>(GamePhaseSequencer_ItemExecuted);        
            m_GamePhaseSequencer.ItemResponseTimerStarted += new Action<IGameSequencerItem>(GamePhaseSequencer_ItemResponseTimerStarted);            
        }        

        void GamePhaseSequencer_ItemExecuted(IGameSequencerItem item, bool success, string msg)
        {            
            if (!success)
            {
                return;
            }
            
            Phase p = item as Phase;
            BroadcastTurnPhaseUpdateToPlayer(p, PacketPhaseUpdate.UpdateKind.Entered);
            
            switch (p.PhaseID)
            {
                case (int)PhaseId.RoundStartup:
                case (int)PhaseId.RoundEnd:
                    p.EndPhase();
                    break;
                
                case (int)PhaseId.Main:
                    OnNextTurnPhase(p);
                    break;

                case (int)PhaseId.BeginTurn:
                case (int)PhaseId.EndTurn:
                    OnNextTurnPhase(p);
                    p.EndPhase(); // we do not linger in the end-ofturn phase. 
                    break;
            }
        }

        void GamePhaseSequencer_ItemResponseTimerStarted(IGameSequencerItem itm)
        {
            Phase p = itm as Phase;
            BroadcastTurnPhaseUpdateToPlayer(p, PacketPhaseUpdate.UpdateKind.EnteredWithDelay);

            DateTime when = new DateTime(p.ResponseTime, DateTimeKind.Utc);
            TimeSpan len = when - DateTime.UtcNow;
            //Log.LogMsg("Broadcast Phase execute Delay for phase [" + p.PhaseName + "] in [" + len.TotalSeconds.ToString() + " seconds].");
        }

        protected void BroadcastTurnPhaseUpdateToPlayer(Phase p, PacketPhaseUpdate.UpdateKind type)
        {
            PacketPhaseUpdate upd = new PacketPhaseUpdate();
            upd.PhaseUpdateKind = type;
            upd.Phase = p;
            Log.LogMsg("Broadcast [" + type.ToString() + "] for phase [" + upd.Phase.PhaseName + "]");
            BroadcastToPlayersInGame(upd, true);            
        }

        #region Network Messages
        
        private void OnPlayerDone(ServerUser user, PacketGameMessage msg)
        {
            try
            {
                PlayerDone(user.CurrentCharacter);
            }
            catch
            {
            }
        }

        protected void SendGameMessageReply(ServerUser client, ReplyType rp, string msg, Packet inResponseToPacket, PropertyBag parms, bool compress, bool encrypt)
        {
            PacketReply rmsg = client.MyConnection.CreateStandardReply(inResponseToPacket, rp, msg);
            if (parms != null)
            {
                rmsg.Parms = parms;
            }
            rmsg.IsCompressed = compress;
            rmsg.IsEncrypted = encrypt;
            inResponseToPacket.ReplyPacket = rmsg;
        }

        protected void SendGameMessageReply(ServerUser client, ReplyType rp, Packet inResponseToPacket, bool compress, bool encrypt)
        {
            SendGameMessageReply(client, rp, "", inResponseToPacket, null, compress, encrypt);
        }
  
        /// <summary>
        /// Tries to end the current phase.
        /// </summary>
        /// <param name="serverCharacterInfo"></param>
        protected virtual void PlayerDone(ServerCharacterInfo serverCharacterInfo)
        {
            if (serverCharacterInfo != null && CurrentPlayer.ID != serverCharacterInfo.ID)
            {
                Log.LogMsg("Player [" + serverCharacterInfo.CharacterName + "] [" + serverCharacterInfo.ID.ToString() + "] tried to say PlayerDone, but it's currently someone else's turn.");
                return;
            }

            if (GamePhaseSequencer.CurrentItem != null)
            {
                
                ((Phase)GamePhaseSequencer.CurrentItem).PlayerDone(serverCharacterInfo);
            }
        }
        
        #endregion

        #region Data

        public GameSequencer GamePhaseSequencer
        {
            get { return m_GamePhaseSequencer; }
            set { m_GamePhaseSequencer = value; }
        }
        private GameSequencer m_GamePhaseSequencer;

        #endregion

        #region Overrides

        protected override void OnGameStarted()
        {
            base.OnGameStarted();

            RegisterGamePacketHandler((int)TurnedGameMessageSubType.PlayerDone, OnPlayerDone);

            ReorderPlayerTurnOrder();
            PacketTurnOrderUpdate update = new PacketTurnOrderUpdate();
            update.CharacterIdOrder = PlayerTurnOrder;
            BroadcastToPlayersInGame(update, true);

            // don't begin the round until all clients are loaded in 
            // BeginNextRound();
        }

        protected override void AllPlayerClientsLoaded()
        {
            base.AllPlayerClientsLoaded();
            BeginNextRound();
        }

        protected override bool OnBeforeHandleGamePacket(ServerUser user, PacketGameMessage msg)
        {
            try
            {
                return IsPlayerPartOfGame(user.CurrentCharacter.ID);
            }
            catch
            {
                return false;
            }
        }

        protected override void OnPlayerRemoved(ServerCharacterInfo character, string reason, bool playerInitiated)
        {
            //Player got removed from game, if it was currently their turn, they are done.
            base.OnPlayerRemoved(character, reason, playerInitiated);
            try
            {
                if (CurrentPlayer != null && CurrentPlayer.ID == character.ID)
                {
                    EndPlayersTurn();
                }
            }
            catch { }
        }

        protected override void OnPlayerAdded(ServerCharacterInfo toon)
        {
            base.OnPlayerAdded(toon);
            PlayerTurnOrder.Remove(toon.ID);
            PlayerTurnOrder.Add(toon.ID);
        }
        
        #endregion

        #region Rounds

        /// <summary>
        /// You may return a delay in ms here to prevent the round from starting immediately. This can allow players 
        /// the opportunity for "Before the beginning of the round..." type of actions. The default is 0.
        /// </summary>
        /// <returns></returns>
        protected virtual int GetRoundStartupDelay()
        {
            return 0;
        }

        /// <summary>
        /// You may return a delay in ms here to prevent the round from ending immediately once all players
        /// in PlayerTurnOrder have taken their turn. This can allow players 
        /// the opportunity for "Before the end of the round..." type of actions. The default is 0.
        /// </summary>
        /// <returns></returns>
        protected virtual int GetRoundEndDelay()
        {
            return 0;
        }

        /// <summary>
        /// Kicks off the next round of turns.
        /// </summary>
        private void BeginNextRound()
        {
            if (PlayerCountSafe < 1)
            {
                // Game has presumably stopped
                Log.LogMsg("No players left in game. Stopping processing.");
                return;
            }

            RoundStartupSequenceItem itm = new RoundStartupSequenceItem(0);
            GamePhaseSequencer.ClearSequence();
            GamePhaseSequencer.AddItem(itm, GetRoundStartupDelay());
            GamePhaseSequencer.ActivateNextItem();
        }

        /// <summary>
        /// Ends the current round, regardless of where it is in the turn sequence.
        /// Ending a round also generally causes the next round to start.
        /// </summary>
        private void EndCurrentRound()
        {
            RoundEndSequenceItem itm = new RoundEndSequenceItem(0);
            GamePhaseSequencer.ClearSequence();
            GamePhaseSequencer.AddItem(itm, GetRoundEndDelay());
            GamePhaseSequencer.ActivateNextItem();
        }

        /// <summary>
        /// Fires before the round ends.
        /// </summary>
        public virtual void OnBeforeRoundEnd(RoundEndSequenceItem item)
        {
        }

        /// <summary>
        /// Fires before the next round round starts up.
        /// </summary>
        /// <param name="item"></param>
        public virtual void OnBeforeNextRound(RoundStartupSequenceItem item)
        {
        }

        /// <summary>
        /// Fires when the next round has begun.  This will cause the turn sequence to initiate.
        /// </summary>
        public virtual void OnNextRoundBegan()
        {
            RoundNumber++;
            // round has started. get the next player's turn and start it up.
            NextPlayersTurn();
        }

        /// <summary>
        /// Fires when the round has ended.  This will cause next round of turns to begin.
        /// </summary>
        public virtual void OnRoundEnded()
        {
            BeginNextRound();
        }

        private int m_RoundNumber;
        /// <summary>
        /// The current round number, i.e. how many times we've gone through the turn cycle.
        /// </summary>
        public int RoundNumber
        {
            get { return m_RoundNumber; }
            set { m_RoundNumber = value; }
        }

        #endregion

        #region Turns
        
        /// <summary>
        /// Who's turn is it? CurrentPlayer's turn!
        /// </summary>
        public ICharacterInfo CurrentPlayer
        {
            get { return m_CurrentPlayer; }
            set 
            { 
                m_CurrentPlayer = value;
                Properties.SetProperty("CurrentPlayer", value.ID);
            }
        }
        private ICharacterInfo m_CurrentPlayer;        

        /// <summary>
        /// An turn-ordered list of character IDs. This is the order in which players are given turns.  You may enter IDs multiple times
        /// to give players multiple turns in a round. If you want to modify the turn order without altering this list, you may override
        /// GetNextTurnOrderIndex and return an index of the next player in this list to take the next turn.
        /// </summary>
        public List<int> PlayerTurnOrder
        {
            get { return m_PlayerTurnOrder; }
            set { m_PlayerTurnOrder = value; }
        }
        private List<int> m_PlayerTurnOrder = new List<int>();
        private int m_TurnOrderIndex = -1;
        
        /// <summary>
        /// Returns an index to PlayerTurnOrder, indicating which player id should get to take the next turn in the round. Returns -1 if the round is ended.
        /// </summary>
        /// <returns></returns>
        protected virtual int GetNextTurnOrderIndex()
        {
            int tmp = m_TurnOrderIndex + 1;
            int toon = -1;

            while (tmp < PlayerTurnOrder.Count)
            {
                toon = PlayerTurnOrder[tmp];
                if (!IsPlayerPartOfGame(toon))
                {
                    PlayerTurnOrder.RemoveAt(tmp);
                }
                else
                {
                    break;
                }
            }

            if (tmp >= PlayerTurnOrder.Count)
            {
                return -1; // everyone took a turn - new round
            }

            return tmp;
        }

        /// <summary>
        /// Rearranges the PlayerTurnOrder array in a random order. Override method to handle more specific ordering of the PlayerTurnOrder.
        /// </summary>
        ///
        protected virtual void ReorderPlayerTurnOrder()
        {
            PlayerTurnOrder.ShuffleFast<int>();            
        }

        /// <summary>
        /// Gets called when we create the initial phase in CurrentPlayer 's turn.  Use this, to override
        /// the standard turn phase chain for a player (BeginnTurn->MainPhase->EndTurn).  
        /// </summary>
        /// <returns></returns>
        protected virtual Phase OnCreateInitialPlayerTurnPhase()
        {
            BeginningOfTurnPhase itm = new BeginningOfTurnPhase(0);            
            return itm;
        }

        /// <summary>
        /// Causes the nextp player in the PlayerTurnOrder to get a turn
        /// </summary>
        public void NextPlayersTurn()
        {
            m_TurnOrderIndex = GetNextTurnOrderIndex();
            if (m_TurnOrderIndex == -1)
            {
                // Next round
                EndCurrentRound();
                return;
            }

            // assign new current player
            ICharacterInfo player = null;
            if (!Players.TryGetValue(PlayerTurnOrder[m_TurnOrderIndex], out player))
            {
                // player got removed since the last few lines of code
                NextPlayersTurn();
            }

            CurrentPlayer = player as ServerCharacterInfo;

            // set up the player's turn phases
            GamePhaseSequencer.ClearSequence();
            Phase itm = OnCreateInitialPlayerTurnPhase();
            GamePhaseSequencer.AddItem(itm, GetTurnPhaseDelay(itm));
            GamePhaseSequencer.ActivateNextItem();
        }

        /// <summary>
        /// Ends the current player's turn.
        /// </summary>
        public void EndPlayersTurn()
        {
            GamePhaseSequencer.ClearSequence();
            EndOfTurnPhase itm = new EndOfTurnPhase(0);
            GamePhaseSequencer.AddItem(itm, GetTurnPhaseDelay(itm));
            GamePhaseSequencer.ActivateNextItem();
        }

        #endregion

        #region Phases

        /// <summary>
        /// Return a delay in miliseconds to assign a delay to a phase, before it actually executes.  This might give other
        /// players an opportunity to respond to that phase.  For instance, you might add an "EndTurnPhase"
        /// with a delay of 15000 ms (15 seconds) to give players the opportunity to say "Before the end of your turn, I will take X action".
        /// </summary>
        /// <param name="p">the phase in question</param>
        /// <returns></returns>
        protected virtual int GetTurnPhaseDelay(Phase p)
        {
            switch (p.PhaseID)
            {
                case (int)PhaseId.BeginTurn:
                    return 0;
                case (int)PhaseId.EndTurn:
                    return 0;
            }
            return 0;
        }

        /// <summary>
        /// Fires before a phase starts.
        /// </summary>
        public virtual void OnBeforeNextTurnPhase(IPhase itm)
        {
            Log.LogMsg("Before " + itm.PhaseName + " for [" + CurrentPlayer.CharacterName + "]");
        }

        /// <summary>
        /// Fires after the phase start delay, if any, elapses
        /// </summary>
        public virtual void OnNextTurnPhase(IPhase itm)
        {
            Log.LogMsg("At " + itm.PhaseName + " for [" + CurrentPlayer.CharacterName + "]");
        }

        /// <summary>
        /// Immediately ends the current phase, as stored in GamePhaseSequencer.
        /// </summary>
        public void EndCurrentTurnPhase(IPhase nextPhase)
        {
            if (nextPhase == null)
            {
                Log.LogMsg("[" + GameID.ToString() + "] couldn't determine which phase should come after [" + nextPhase.PhaseName + "|" + nextPhase.PhaseID.ToString() + "]. Game is likely stuck now.");
                return;
            }

            GamePhaseSequencer.ClearSequence();
            GamePhaseSequencer.AddItem(nextPhase as Phase, GetTurnPhaseDelay(nextPhase as Phase));
            GamePhaseSequencer.ActivateNextItem();
        }

        #endregion              

        #region Dispose
        bool m_Disposed = false;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!m_Disposed)
            {
                if (disposing)
                {
                    if (GamePhaseSequencer != null)
                    {
                        GamePhaseSequencer.Dispose();
                        GamePhaseSequencer = null;
                    }
                }

                // There are no unmanaged resources to release, but
                // if we add them, they need to be released here.
            }
            m_Disposed = true;

            // If it is available, make the call to the
            // base class's Dispose(Boolean) method
            //base.Disposed(disposing);
        }
        #endregion
    }
}
