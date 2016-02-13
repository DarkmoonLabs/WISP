using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace Shared
{
      /// <summary>
    ///  Represent one connection to a type of central server
    /// </summary>
    public class ClientCentralServerOutboundConnection : ClientServerOutboundConnection
    {
        #region CharacterListingArrived Event
        private EventHandler CharacterListingArrivedInvoker;

        /// <summary>
        /// Fires when we get a listing of characters from central server.  The actual character data will be stored in the
        /// Characters property for this object.
        /// </summary>
        public event EventHandler CharacterListingArrived
        {
            add
            {
                AddHandler_CharacterListingArrived(value);
            }
            remove
            {
                RemoveHandler_CharacterListingArrived(value);
            }
        }

        
        private void AddHandler_CharacterListingArrived(EventHandler value)
        {
            CharacterListingArrivedInvoker = (EventHandler)Delegate.Combine(CharacterListingArrivedInvoker, value);
        }

        
        private void RemoveHandler_CharacterListingArrived(EventHandler value)
        {
            CharacterListingArrivedInvoker = (EventHandler)Delegate.Remove(CharacterListingArrivedInvoker, value);
        }

        private void FireCharacterListingArrived(object sender, EventArgs args)
        {
            if (CharacterListingArrivedInvoker != null)
            {
                CharacterListingArrivedInvoker(sender, args);
            }
        }
        #endregion

        #region CharacterActivated Event
        private EventHandler CharacterActivatedInvoker;

        /// <summary>
        /// Fires when the server has acknowledged and activated for play the character we selected. The character data will be
        /// present in the CurrentCharacter property.
        /// </summary>
        public event EventHandler CharacterActivated
        {
            add
            {
                AddHandler_CharacterActivated(value);
            }
            remove
            {
                RemoveHandler_CharacterActivated(value);
            }
        }

        
        private void AddHandler_CharacterActivated(EventHandler value)
        {
            CharacterActivatedInvoker = (EventHandler)Delegate.Combine(CharacterActivatedInvoker, value);
        }

        
        private void RemoveHandler_CharacterActivated(EventHandler value)
        {
            CharacterActivatedInvoker = (EventHandler)Delegate.Remove(CharacterActivatedInvoker, value);
        }

        private void FireCharacterActivated(object sender, EventArgs args)
        {
            if (CharacterActivatedInvoker != null)
            {
                CharacterActivatedInvoker(sender, args);
            }
        }
        #endregion
       
        #region CreateCharacterFailed Event
        private Action<string> CreateCharacterFailedInvoker;

        /// <summary>
        /// Fires in response to a call to CreateCharacter.  Returns a message on failure
        /// </summary>
        public event Action<string> CreateCharacterFailed
        {
            add
            {
                AddHandler_CreateCharacterFailed(value);
            }
            remove
            {
                RemoveHandler_CreateCharacterFailed(value);
            }
        }

        
        private void AddHandler_CreateCharacterFailed(Action<string> value)
        {
            CreateCharacterFailedInvoker = (Action<string>)Delegate.Combine(CreateCharacterFailedInvoker, value);
        }

        
        private void RemoveHandler_CreateCharacterFailed(Action<string> value)
        {
            CreateCharacterFailedInvoker = (Action<string>)Delegate.Remove(CreateCharacterFailedInvoker, value);
        }

        private void FireCreateCharacterFailed(string msg)
        {
            if (CreateCharacterFailedInvoker != null)
            {
                CreateCharacterFailedInvoker(msg);
            }
        }
        #endregion
        
        #region SelectCharacterFailed Event
        private Action<string> SelectCharacterFailedInvoker;

        /// <summary>
        /// Fires in response to a call to SelectCharacter.  Returns a message on failure
        /// </summary>
        public event Action<string> SelectCharacterFailed
        {
            add
            {
                AddHandler_SelectCharacterFailed(value);
            }
            remove
            {
                RemoveHandler_SelectCharacterFailed(value);
            }
        }

        
        private void AddHandler_SelectCharacterFailed(Action<string> value)
        {
            SelectCharacterFailedInvoker = (Action<string>)Delegate.Combine(SelectCharacterFailedInvoker, value);
        }

        
        private void RemoveHandler_SelectCharacterFailed(Action<string> value)
        {
            SelectCharacterFailedInvoker = (Action<string>)Delegate.Remove(SelectCharacterFailedInvoker, value);
        }

        private void FireSelectCharacterFailed(string msg)
        {
            if (SelectCharacterFailedInvoker != null)
            {
                SelectCharacterFailedInvoker(msg);
            }
        }
        #endregion

        #region DeleteCharacterFailed Event
        private Action<string> DeleteCharacterFailedInvoker;

        /// <summary>
        /// Fires in response to a call to DeleteCharacter.  Returns a message on failure
        /// </summary>
        public event Action<string> DeleteCharacterFailed
        {
            add
            {
                AddHandler_DeleteCharacterFailed(value);
            }
            remove
            {
                RemoveHandler_DeleteCharacterFailed(value);
            }
        }

        
        private void AddHandler_DeleteCharacterFailed(Action<string> value)
        {
            DeleteCharacterFailedInvoker = (Action<string>)Delegate.Combine(DeleteCharacterFailedInvoker, value);
        }

        
        private void RemoveHandler_DeleteCharacterFailed(Action<string> value)
        {
            DeleteCharacterFailedInvoker = (Action<string>)Delegate.Remove(DeleteCharacterFailedInvoker, value);
        }

        private void FireDeleteCharacterFailed(string msg)
        {
            if (DeleteCharacterFailedInvoker != null)
            {
                DeleteCharacterFailedInvoker(msg);
            }
        }
        #endregion

        /// <summary>
        /// A list of all characters, based on what Central server reported, that we have on this account
        /// </summary>
        public List<CharacterInfo> Characters { get; set; }

        private CharacterInfo m_CurrentCharacter = null;
        /// <summary>
        /// The currently selected character, according to the server.
        /// </summary>
        public CharacterInfo CurrentCharacter
        {
            get { return m_CurrentCharacter; }
            set { m_CurrentCharacter = value; }
        }
        

        public ClientCentralServerOutboundConnection(bool isBlocking) : base(isBlocking)
        {
            Characters = new List<CharacterInfo>();
            RegisterPacketHandler((int)PacketType.PacketGenericMessage,(int)GenericMessageType.CharacterActivated, OnCharacterActivated);
            RegisterPacketHandler((int)PacketType.PacketRijndaelExchangeRequest, delegate { Client.ConnectionPhase = ClientConnectionPhase.CentralServerGreeting; });

            // Reply to CreateCharacter, DeleteCharacter, SelectCharacter and CharacterListing
            RegisterStandardPacketReplyHandler((int)PacketType.PacketGenericMessage, (int)GenericMessageType.RequestSelectCharacter, OnRequestSelectCharacterReply);
            RegisterStandardPacketReplyHandler((int)PacketType.PacketGenericMessage, (int)GenericMessageType.RequestDeleteCharacter, OnRequestDeleteCharacterReply);
            RegisterStandardPacketReplyHandler((int)PacketType.PacketGenericMessage, (int)GenericMessageType.RequestCreateCharacter, OnRequestCreateCharacterReply);

            //Character listing doesnt arrive via PacketReply, it's its own packet
            RegisterPacketHandler((int)PacketType.CharacterListing, OnCharacterListingReceived);
        }

        protected override void OnSocketKilled(string msg)
        {
            base.OnSocketKilled(msg);
        }

        private static bool m_IsInitialized = false;
        protected override void OnInitialize()
        {
            base.OnInitialize();
            if (!m_IsInitialized)
            {
                NetworkConnection.RegisterPacketCreationDelegate((int)PacketType.CharacterListing, delegate { return new PacketCharacterListing(); });
                m_IsInitialized = true;
            }
        }

        private void OnCharacterActivated(INetworkConnection con, Packet gmsg)
        {
            PacketGenericMessage msg = gmsg as PacketGenericMessage;
            CharacterInfo ci = msg.Parms.GetComponentProperty((int)PropertyID.CharacterInfo) as CharacterInfo;
            CurrentCharacter = ci;
            FireCharacterActivated(this, EventArgs.Empty);            
        }

        protected override void OnConnected(bool success, string msg)
        {
            if (success)
            {
                Client.ConnectionPhase = ClientConnectionPhase.CentralServerConnected;
            }
            base.OnConnected(success, msg);
        }

        /// <summary>
        /// Gets called when we get a listing of characters on this cluster.  This only happens if the server is configured to use Characters.  If the server
        /// does not use characters, this message will never arrive.  If it does, we need to pick a character (or create a new one and then pick one) before
        /// central server will let us get to any of the content on the server.
        /// </summary>
        /// <param name="con"></param>
        /// <param name="p"></param>
        protected virtual void OnCharacterListingReceived(INetworkConnection con, Packet p)
        {
            PacketCharacterListing msg = p as PacketCharacterListing;
            Characters.Clear();
            for(int i = 0; i < msg.Characters.Count; i++)
            {
                Characters.Add(msg.Characters[i] as CharacterInfo);
            }
            FireCharacterListingArrived(this, EventArgs.Empty);
        }

        /// <summary>
        /// Petitiion central server to create a new character.  The response will arrive via the 
        /// CreateCharacterFailed event on failure.  On success a new character listing with the new character will be sent.
        /// </summary>
        /// <param name="proposedCharacter"></param>
        public bool CreateCharacter(PropertyBag characterProps)
        {            
            SendGenericMessage((int)GenericMessageType.RequestCreateCharacter, characterProps, false);
            return true;
        }

        /// <summary>
        /// Deletes a character.  If successfull, a new charcter listing without the target character is sent.  On failure, the DeleteCharacterFailed event will fire.
        /// </summary>
        /// <param name="characterId">the id of the character to delete</param>
        /// <returns></returns>
        public bool DeleteCharacter(int characterId)
        {
            PropertyBag bag = new PropertyBag();
            bag.SetProperty((int)PropertyID.CharacterId, characterId);
            SendGenericMessage((int)GenericMessageType.RequestDeleteCharacter, bag, false);
            return true;
        }

        /// <summary>
        /// Selects a character to play with.  
        /// </summary>
        /// <param name="characterId"></param>
        /// <returns></returns>
        public bool SelectCharacter(int characterId)
        {
            PropertyBag bag = new PropertyBag();
            bag.SetProperty((int)PropertyID.CharacterId, characterId);
            SendGenericMessage((int)GenericMessageType.RequestSelectCharacter, bag, false);
            return true;
        }

        /// <summary>
        /// Requests a full listings of all characters that we have on the server.
        /// </summary>
        /// <returns></returns>
        public bool RequestCharacterListing()
        {
            SendGenericMessage((int)GenericMessageType.RequestCharacterListing, false);
            return true;
        }

        protected virtual void OnRequestCreateCharacterReply(INetworkConnection con, Packet reply)
        {
            PacketReply p = reply as PacketReply;
            if (p.ReplyCode != ReplyType.OK)
            {
                FireCreateCharacterFailed(p.ReplyMessage);
            }
            else
            {
                RequestCharacterListing();
            }
        }

        protected virtual void OnRequestDeleteCharacterReply(INetworkConnection con, Packet reply)
        {
            PacketReply p = reply as PacketReply;        
            if (p.ReplyCode != ReplyType.OK)
            {
                FireDeleteCharacterFailed(p.ReplyMessage);
            }
            else
            {
                RequestCharacterListing();
            }
        }

        protected virtual void OnRequestSelectCharacterReply(INetworkConnection con, Packet reply)
        {
            PacketReply p = reply as PacketReply;
            if (p.ReplyCode != ReplyType.OK)
            {
                FireSelectCharacterFailed(p.ReplyMessage);
            }
            else if (p.ReplyCode == ReplyType.OK)
            {
                CurrentCharacter = p.Parms.GetComponentProperty((int)PropertyID.CharacterInfo) as CharacterInfo;
                FireCharacterActivated(this, EventArgs.Empty);
            }
        }

    

    }
}
