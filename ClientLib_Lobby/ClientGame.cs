using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared
{
    public class ClientGame : Game
    {
        public ClientGameServerOutboundConnection Connection;
        public ClientGame(Game g, LobbyClient client, ClientGameServerOutboundConnection con)
        {
            Connection = con;
            this.Client = client;
            this.AbandonedTimestamp = g.AbandonedTimestamp;
            this.Players = g.Players;
            this.GameID = g.GameID;
            this.IsShuttingDown = g.IsShuttingDown;
            this.Observers = g.Observers;
            this.Owner = g.Owner;
            this.Properties = g.Properties;

            RegisterGamePacketHandler((int)LobbyGameMessageSubType.GameInfoNotification, OnGameInfoNotification);
            RegisterGamePacketHandler((int)LobbyGameMessageSubType.GamePropertiesUpdateNotification, OnGamePropertiesUpdate);
            RegisterGamePacketHandler((int)LobbyGameMessageSubType.Chat, OnGameChat);
            RegisterGamePacketHandler((int)LobbyGameMessageSubType.NewOwner, OnNewGameOwner);            
        }

        /// <summary>
        /// Synchronized clock.
        /// </summary>
        public NetworkClock Clock
        {
            get
            {
                if (Client.GameServer != null)
                {
                    return Client.GameServer.Clock;
                }
                return null;
            }
        }

         /// <summary>
         /// Send a chat message to the game.
         /// </summary>
         /// <param name="targetPlayer">-1 for public chat, character id to whisper</param>
         /// <param name="text">the text to send</param>
        public void SendChat(int targetPlayer, string text)
        {
            if (!Client.GameServerReadyForPlay)
            {
                Log.LogMsg("Tried to send 'Chat', but game server not ready.");
                return;
            }

            PacketGameMessage chat = new PacketGameMessage();
            chat.PacketSubTypeID = (int)LobbyGameMessageSubType.Chat;
            chat.Parms.SetProperty("target", targetPlayer);
            chat.Parms.SetProperty("text", text);
            Connection.Send(chat);
        }

        /// <summary>
        /// Sends a PacketGameMessage to the game server for this particular game.  All PacketGameMessages are handled serially on the server in the
        /// order that they were received. 
        /// </summary>
        /// <param name="gamePacketSubType">packet type ID</param>
        /// <param name="parms">any paramters</param>
        public void SendGameMessage(int gamePacketSubType, PropertyBag parms = null)
        {
            if (!Client.GameServerReadyForPlay)
            {
                Log.LogMsg("Tried to send PacketGameMessage type [ " + gamePacketSubType.ToString() + "], but game server not ready.");
                return;
            }

            PacketGameMessage gm = new PacketGameMessage();
            gm.PacketSubTypeID = gamePacketSubType;
            gm.Parms = parms;
            Connection.Send(gm);
        }

        /// <summary>
        /// Notifies the server of far along we are in loading the level after the game has started.
        /// No one can take any moves until all clients are loaded in.
        /// </summary>
        /// <param name="percent">the percentage between 0.0f and 100.0f</param>
        public void NotifyPercentLevelLoaded(float percent)
        {
            if (!Client.GameServerReadyForPlay)
            {
                Log.LogMsg("Tried to send PacketGameMessage type [ ClientLevelLoaded ], but game server not ready.");
                return;
            }

            PropertyBag p = new PropertyBag();
            p.SetProperty("PercentLoaded", Math.Max(0f, percent));
            SendGameMessage((int)LobbyGameMessageSubType.ClientLevelLoaded, p);
        }

        /// <summary>
        /// While the game is in the lobby, you may request a new seat/team/etc.  The exact seat/team designation depends on the type of game being played.
        /// </summary>
        /// <param name="props">the requested setup arrangement</param>
        public void RequestNewSeating(PropertyBag props)
        {
            PacketGameMessage seat = new PacketGameMessage();
            seat.PacketSubTypeID = (int)LobbyGameMessageSubType.SeatChangeRequest;
            seat.Parms = props;
            Connection.Send(seat);
        }

        /// <summary>
        /// Requests that the current game be kicked off.
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public bool StartGame(ref string msg)
        {
            if (!Client.IsGameServerConnected || !Client.GameServerReadyForPlay)
            {
                msg = "Not ready to communicate with game server.";
                return false;
            }

            if (Client.CurrentGame != this)
            {
                msg = "Not currently part of a game.";
                return false;
            }

            Connection.SendGenericMessage((int)GenericLobbyMessageType.RequestStartGame);
            return true;
        }

        /// <summary>
        /// Request the leaving of an existing game instance on the game server.  
        /// This is a non-blocking call.  Once the game leave request has been resolved
        /// the LobbyClient.PlayerLeft event will fire
        /// </summary>
        /// <param name="msg">a string which will hold player facing text message if the call fails (returns false)</param>
        /// <returns>true if IsGameServerConnected and was ReadyForPlay and the request was thus sent</returns>
        public bool RequestLeaveGame(ref string msg)
        {
            msg = "";
            if (!Client.IsGameServerConnected || !Client.GameServerReadyForPlay)
            {
                msg = "Not ready to communicate with game server.";
                return false;
            }

            Log.LogMsg("Requesting to leave game.");
            Connection.SendGenericMessage((int)GenericLobbyMessageType.LeaveGame, false);
            return true;
        }

        protected virtual void OnNewGameOwner(INetworkConnection con, Packet msg)
        {
            PacketGameMessage gmsg = msg as PacketGameMessage;
            if (gmsg == null)
            {
                return;
            }

            int newOwner = gmsg.Parms.GetIntProperty("NewOwner").GetValueOrDefault(-1);
            if (newOwner != -1)
            {
                Owner = newOwner;
            }
        }

        protected virtual void OnGameChat(INetworkConnection con, Packet msg)
        {
            PacketGameMessage gmsg = msg as PacketGameMessage;
            if(gmsg == null)
            {
                return;
            }

            CharacterInfo from = gmsg.Parms.GetWispProperty("sender") as CharacterInfo;
            string text =  gmsg.Parms.GetStringProperty("text");
            int target = gmsg.Parms.GetIntProperty("target").GetValueOrDefault(-1);
            if(target == -1)
            {
                // public message
                AddMessage(FormatString(GameStringType.PlayerName, from.CharacterName + ": ") + FormatString(GameStringType.ChatText, text));
            }
            else
            {
                AddMessage(FormatString(GameStringType.PlayerName, from.CharacterName + " whispered to you: ") + FormatString(GameStringType.PrivateMessage, text));                 
                // private message
            }           
        }       

        private void OnGameInfoNotification(INetworkConnection con, Packet msg)
        {
            PacketGameInfoNotification info = msg as PacketGameInfoNotification;
            if (info != null)
            {
                OnGameInfoNotification(con, info);
            }
        }

        protected virtual void OnGameInfoNotification(INetworkConnection con, PacketGameInfoNotification msg)
        {
            Log.LogMsg(">>> Game Info Message: " + msg.Message);
            AddMessage(FormatString(GameStringType.SystemMessage, "-->" + msg.Message));
        }

        private void OnGamePropertiesUpdate(INetworkConnection con, Packet msg)
        {
            PacketGamePropertiesUpdateNotification prop = msg as PacketGamePropertiesUpdateNotification;
            if (prop != null)
            {
                OnGamePropertiesUpdate(con, prop);
            }
        }

        protected virtual void OnGamePropertiesUpdate(INetworkConnection con, PacketGamePropertiesUpdateNotification msg)
        {
            if (msg.PropertyBagId == Properties.ID)
            {
                if (msg.Remove)
                {
                    Properties.RemoveProperties(msg.Properties);
                }
                else
                {
                    Properties.UpdateWithValues(msg.Properties);
                }
            }

            Log.LogMsg(">>> Game Properties Updated: " + msg.Properties.Length.ToString());
        }
                    
        private PacketHandlerMap m_PacketHandlers = new PacketHandlerMap();
        private PacketHandlerMap m_GameReplyHandlerMap = new PacketHandlerMap();

        /// <summary>
        /// The client that is servicing this game
        /// </summary>
        public LobbyClient Client { get; private set; }

        /// <summary>
        /// Adds a packet to the queue for serial processing.  If the con.ProcessPacketsImmediately is set to true, then we will not queue the packet
        /// but rather will execute the handler immediately.
        /// </summary>
        /// <param name="con">the connection which sent the packet</param>
        /// <param name="msg">the packet</param>
        public void HandleGamePacketReply(LobbyClientGameServerOutboundConnection con, PacketReply msg)
        {
            HandlePacket(con, msg, m_GameReplyHandlerMap);
        }

        private void HandlePacket(LobbyClientGameServerOutboundConnection con, PacketReply msg, PacketHandlerMap map)
        {
            Action<INetworkConnection, Packet> handler = map.GetHandlerDelegate(msg.PacketSubTypeID);
            if (handler != null)
            {
                try
                {
                    handler(con, msg);
                }
                catch (Exception e)
                {
                    Log.LogMsg("Exception thrown whilst processing game packet type " + msg.PacketTypeID.ToString() + ", sub-type " + msg.PacketSubTypeID + ". Object = " + this.GetType().ToString() + ", Message: " + e.Message + ". Stack:\r\n " + e.StackTrace);
                }

                con.OnAfterPacketProcessed(msg);
                return;
            }

            con.KillConnection("Did not have a registered game packet handler for game packet. " + msg.PacketTypeID.ToString() + ", SubType " + msg.PacketSubTypeID.ToString() + ". ");
        }

        /// <summary>
        /// Adds a packet to the queue for serial processing.  If the con.ProcessPacketsImmediately is set to true, then we will not queue the packet
        /// but rather will execute the handler immediately.
        /// </summary>
        /// <param name="con">the connection which sent the packet</param>
        /// <param name="msg">the packet</param>
        public void HandleGamePacket(LobbyClientGameServerOutboundConnection con, PacketGameMessage msg)
        {
            HandlePacket(con, msg, m_PacketHandlers);
        }

        /// <summary>
        /// Register a particular packet for this game
        /// </summary>
        /// <param name="packetKind"></param>
        /// <param name="handler"></param>
        protected void RegisterGamePacketHandler(int packetKind, Action<INetworkConnection, Packet> handler)
        {
            m_PacketHandlers.RegisterHandler(packetKind, handler);
        }

        /// <summary>
        /// Unregister a particular packet for this game
        /// </summary>
        /// <param name="packetKind"></param>
        /// <param name="handler"></param>
        protected void UnregisterGamePacketHandler(int packetKind, Action<INetworkConnection, Packet> handler)
        {
            m_PacketHandlers.UnregisterHandler(packetKind, handler);
        }

        /// <summary>
        /// Register a particular packet reply for this game
        /// </summary>
        /// <param name="packetKind"></param>
        /// <param name="handler"></param>
        protected void RegisterGamePacketReplyHandler(int packetKind, Action<INetworkConnection, Packet> handler)
        {
            m_GameReplyHandlerMap.RegisterHandler(packetKind, handler);
        }

        /// <summary>
        /// Unregister a particular packet reply for this game
        /// </summary>
        /// <param name="packetKind"></param>
        /// <param name="handler"></param>
        protected void UnregisterGamePacketReplyHandler(int packetKind, Action<INetworkConnection, Packet> handler)
        {
            m_GameReplyHandlerMap.UnregisterHandler(packetKind, handler);
        }       

    }
}
