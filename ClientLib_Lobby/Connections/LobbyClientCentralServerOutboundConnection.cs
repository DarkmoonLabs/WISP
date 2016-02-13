using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace Shared
{
    public delegate void CompleteGameListingArrivedDelegate(bool success, string msg, LobbyClientCentralServerOutboundConnection sender, List<Game> games, int totalGames);
    public delegate void JoinGameResultDelegate(bool result, string msg, ClientServerOutboundConnection sender, Game g);
    public delegate void QuickMatchResultArrivedDelegate(bool result, string msg, LobbyClientCentralServerOutboundConnection sender, Game g);  
    
    /// <summary>
    ///  Represent one connection to a lobby type game server
    /// </summary>
    public class LobbyClientCentralServerOutboundConnection : ClientCentralServerOutboundConnection
    {
        #region QuickMatchResultArrived Event
        private QuickMatchResultArrivedDelegate QuickMatchResultArrivedInvoker;

        /// <summary>
        /// Fires when get a response to a quick match request
        /// </summary>
        public event QuickMatchResultArrivedDelegate QuickMatchResultArrived
        {
            add
            {
                AddHandler_QuickMatchResultArrived(value);
            }
            remove
            {
                RemoveHandler_QuickMatchResultArrived(value);
            }
        }

        private void AddHandler_QuickMatchResultArrived(QuickMatchResultArrivedDelegate value)
        {
            QuickMatchResultArrivedInvoker = (QuickMatchResultArrivedDelegate)Delegate.Combine(QuickMatchResultArrivedInvoker, value);
        }

        private void RemoveHandler_QuickMatchResultArrived(QuickMatchResultArrivedDelegate value)
        {
            QuickMatchResultArrivedInvoker = (QuickMatchResultArrivedDelegate)Delegate.Remove(QuickMatchResultArrivedInvoker, value);
        }

        private void FireQuickMatchResultArrived(bool result, string msg, LobbyClientCentralServerOutboundConnection sender, Game g)
        {
            if (QuickMatchResultArrivedInvoker != null)
            {
                QuickMatchResultArrivedInvoker(result, msg, sender, g);
            }
        }
        #endregion

        #region CompleteMatchListingArrived Event
        private CompleteGameListingArrivedDelegate CompleteMatchListingArrivedInvoker;

        /// <summary>
        /// Fires when get a response to a match listing request
        /// </summary>
        public event CompleteGameListingArrivedDelegate CompleteMatchListingArrived
        {
            add
            {
                AddHandler_CompleteMatchListingArrived(value);
            }
            remove
            {
                RemoveHandler_CompleteMatchListingArrived(value);
            }
        }

        
        private void AddHandler_CompleteMatchListingArrived(CompleteGameListingArrivedDelegate value)
        {
            CompleteMatchListingArrivedInvoker = (CompleteGameListingArrivedDelegate)Delegate.Combine(CompleteMatchListingArrivedInvoker, value);
        }

        
        private void RemoveHandler_CompleteMatchListingArrived(CompleteGameListingArrivedDelegate value)
        {
            CompleteMatchListingArrivedInvoker = (CompleteGameListingArrivedDelegate)Delegate.Remove(CompleteMatchListingArrivedInvoker, value);
        }

        private void FireCompleteMatchListingArrived(bool result, string msg, LobbyClientCentralServerOutboundConnection con, List<Game> games, int totalGames)
        {
            if (CompleteMatchListingArrivedInvoker != null)
            {
                CompleteMatchListingArrivedInvoker(result, msg, con, games, totalGames);
            }
        }
        #endregion

        #region CentralCreateGameRequestResolved Event
        private CreateGameRequestResolvedDelegate CentralCreateGameRequestResolvedInvoker;

        /// <summary>
        /// Fires when we get a reply to create a game on the Central Server - note the game is not yet created, even if the response to this is positive.
        /// The player still needs to be transferred to the actual game server that will host the new game.
        /// </summary>
        public event CreateGameRequestResolvedDelegate CentralCreateGameRequestResolved
        {
            add
            {
                AddHandler_CentralCreateGameRequestResolved(value);
            }
            remove
            {
                RemoveHandler_CentralCreateGameRequestResolved(value);
            }
        }

        
        private void AddHandler_CentralCreateGameRequestResolved(CreateGameRequestResolvedDelegate value)
        {
            CentralCreateGameRequestResolvedInvoker = (CreateGameRequestResolvedDelegate)Delegate.Combine(CentralCreateGameRequestResolvedInvoker, value);
        }

        
        private void RemoveHandler_CentralCreateGameRequestResolved(CreateGameRequestResolvedDelegate value)
        {
            CentralCreateGameRequestResolvedInvoker = (CreateGameRequestResolvedDelegate)Delegate.Remove(CentralCreateGameRequestResolvedInvoker, value);
        }

        private void FireCentralCreateGameRequestResolved(bool result, string msg, ClientServerOutboundConnection con, Game newGame)
        {
            if (CentralCreateGameRequestResolvedInvoker != null)
            {
                CentralCreateGameRequestResolvedInvoker(result, msg, con, newGame);
            }
        }
        #endregion

        #region JoinGameResolved Event
        private JoinGameResultDelegate JoinGameResolvedInvoker;

        /// <summary>
        /// Fires when the central server responds to our request to join a game.  Note that the player still needs to be transferred to the actual 
        /// game server hosting the requested game before the player is considered "in the game".  Game server might reject the player when he gets there
        /// for a number of reasons (game filled up, etc).
        /// </summary>
        public event JoinGameResultDelegate JoinGameResolved
        {
            add
            {
                AddHandler_JoinGameResolved(value);
            }
            remove
            {
                RemoveHandler_JoinGameResolved(value);
            }
        }

        
        private void AddHandler_JoinGameResolved(JoinGameResultDelegate value)
        {
            JoinGameResolvedInvoker = (JoinGameResultDelegate)Delegate.Combine(JoinGameResolvedInvoker, value);
        }

        
        private void RemoveHandler_JoinGameResolved(JoinGameResultDelegate value)
        {
            JoinGameResolvedInvoker = (JoinGameResultDelegate)Delegate.Remove(JoinGameResolvedInvoker, value);
        }

        private void FireJoinGameResolved(bool result, string msg, ClientServerOutboundConnection con, Game g)
        {
            if (JoinGameResolvedInvoker != null)
            {
                JoinGameResolvedInvoker(result, msg, con, g);
            }
        }
        #endregion

        public LobbyClientCentralServerOutboundConnection(bool isBlocking) : base(isBlocking)
        {
            RegisterPacketHandler((int)LobbyPacketType.MatchRefresh, OnCompleteGameListingArrived);
            RegisterPacketHandler((int)LobbyPacketType.MatchNotification, OnGameNotification);
            RegisterPacketHandler((int)LobbyPacketType.QuickMatchResult, OnQuickMatchResult);
        }

        protected void OnGameNotification(INetworkConnection con, Packet p)
        {
            PacketMatchNotification note = p as PacketMatchNotification;
            switch (note.Kind)
            {
                case MatchNotificationType.MatchCreated:
                    OnGameCreated(note);
                    break;
                case MatchNotificationType.PlayerAdded:
                    OnPlayerAdded(note);
                    break;
                default:
                    Log.LogMsg("Unhandled packet match notification [" + note.Kind.ToString() + "]");
                    break;
            }
        }

        private void OnPlayerAdded(PacketMatchNotification note)
        {
            // PlayerAdded messages from Central only come in responds to a join request - there are no actual games hosted on Central.
            // Therefore, we interpret the PlayerAdded message a JoinGameRequest result.
            FireJoinGameResolved(note.ReplyCode == ReplyType.OK, note.ReplyMessage, this, note.TheGame as Game);
        }

        protected virtual void OnGameCreated(PacketMatchNotification note)
        {
            if (note.TheGame != null)
            {
                Log.LogMsg("Game [" + note.TheGame.GameID.ToString() + "] creation permission granted?: " + note.ReplyCode.ToString() + " - " + note.ReplyMessage);
            }
            else
            {
                Log.LogMsg("Game creation permission granted? : " + note.ReplyCode.ToString() + " - " + note.ReplyMessage);
            }

            FireCentralCreateGameRequestResolved(note.ReplyCode == ReplyType.OK, note.ReplyMessage, this, note.TheGame as Game);
        }

        private static bool m_IsInitialized = false;
        protected override void OnInitialize()
        {
            base.OnInitialize();
            if (!m_IsInitialized)
            {
                NetworkConnection.RegisterPacketCreationDelegate((int)LobbyPacketType.MatchNotification, delegate { return new PacketMatchNotification(); });
                NetworkConnection.RegisterPacketCreationDelegate((int)LobbyPacketType.MatchRefresh, delegate { return new PacketMatchRefresh(); });
                NetworkConnection.RegisterPacketCreationDelegate((int)LobbyPacketType.QuickMatchResult, delegate { return new PacketQuickMatchResult(); });
                
                m_IsInitialized = true;
            }
        }

        protected override void OnSocketKilled(string msg)
        {
            base.OnSocketKilled(msg);
        }
        
        protected virtual void OnCompleteGameListingArrived(INetworkConnection con, Packet p)
        {
            PacketMatchRefresh packetMatchRefresh = p as PacketMatchRefresh;
            int totalGames = packetMatchRefresh.Parms.GetIntProperty("TotalGames").GetValueOrDefault(-1);
            FireCompleteMatchListingArrived(packetMatchRefresh.ReplyCode == ReplyType.OK, packetMatchRefresh.ReplyMessage, this, packetMatchRefresh.TheGames, totalGames);
        }

        protected virtual void OnQuickMatchResult(INetworkConnection con, Packet p)
        {
            PacketQuickMatchResult res = p as PacketQuickMatchResult;
            FireQuickMatchResultArrived(res.ReplyCode == ReplyType.OK, res.ReplyMessage, this, res.TheGame);
        }
      



    }
}
