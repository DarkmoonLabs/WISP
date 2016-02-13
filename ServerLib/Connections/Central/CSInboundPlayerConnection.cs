using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO.Compression;
using System.Timers;
using System.Data.SqlClient;

namespace Shared
{
    /// <summary>
    /// Represents one connection to the server, by a player. Central server is the arbiter of content on the server cluster.
    /// This class handles the authentication ticket based player login procedure. If the connecting player has been previously
    /// authorized via a transfer request from the login server or the player does not present the correct authentication ticket
    /// during the login procedure, this connection will be closed.
    /// </summary>
    public class CSInboundPlayerConnection : InboundPlayerConnection
    {
        public CSInboundPlayerConnection(Socket s, ServerBase server, bool isBlocking)
            : base(s, server, isBlocking)
        {            
        }

        private static bool m_IsInitialized = false;
        protected override void OnInitialize()
        {
            base.OnInitialize();            

            if (!m_IsInitialized)
            {
                CharacterUtil.Instance.CharacterMinNameLength = ConfigHelper.GetIntConfig("CharacterMinNameLength", 3);
                CharacterUtil.Instance.LoadCharacterTemplate();
                m_IsInitialized = true; 
            }

            CharacterUtil.Instance.CharacterDeleting += new Func<System.Data.SqlClient.SqlConnection, System.Data.SqlClient.SqlTransaction, int, ServerUser, bool>(CharacterUtil_OnCharacterDeleting);
            CharacterUtil.Instance.CharacterPersisting += new Func<SqlConnection, SqlTransaction, ServerCharacterInfo, bool>(CharacterUtil_CharacterPersisting);
            CharacterUtil.Instance.CharacterLoading += new Func<SqlConnection, SqlTransaction, ServerCharacterInfo, bool>(CharacterUtil_CharacterLoading);
            CharacterUtil.Instance.CharacterSaving += new Func<SqlConnection, SqlTransaction, ServerCharacterInfo, bool>(CharacterUtil_CharacterSaving);
        }
       
        protected override void OnSocketKilled(string msg)
        {
            if (ServerUser.CurrentCharacter != null)
            {
                CharacterCache.UncacheCharacter(ServerUser.CurrentCharacter.CharacterInfo.ID);
            }

            CharacterUtil.Instance.CharacterDeleting -= new Func<System.Data.SqlClient.SqlConnection, System.Data.SqlClient.SqlTransaction, int, ServerUser, bool>(CharacterUtil_OnCharacterDeleting);
            CharacterUtil.Instance.CharacterPersisting -= new Func<SqlConnection, SqlTransaction, ServerCharacterInfo, bool>(CharacterUtil_CharacterPersisting);
            CharacterUtil.Instance.CharacterLoading -= new Func<SqlConnection, SqlTransaction, ServerCharacterInfo, bool>(CharacterUtil_CharacterLoading);
            CharacterUtil.Instance.CharacterSaving -= new Func<SqlConnection, SqlTransaction, ServerCharacterInfo, bool>(CharacterUtil_CharacterSaving);
            base.OnSocketKilled(msg);
        }

        protected virtual bool OnCharacterLoading(SqlConnection con, SqlTransaction tran, ServerCharacterInfo ci)
        {
            return true;
        }

        bool CharacterUtil_CharacterLoading(SqlConnection con, SqlTransaction tran, ServerCharacterInfo ci)
        {
            return OnCharacterLoading(con, tran, ci);
        }

        protected virtual bool OnCharacterSaving(SqlConnection con, SqlTransaction tran, ServerCharacterInfo ci)
        {
            return true;
        }

        bool CharacterUtil_CharacterSaving(SqlConnection con, SqlTransaction tran, ServerCharacterInfo ci)
        {
            return OnCharacterSaving(con, tran, ci);
        }

        protected virtual bool OnCharacterPersisting(SqlConnection con, SqlTransaction tran, ServerCharacterInfo ci)
        {
            return true;
        }

        private bool CharacterUtil_CharacterPersisting(SqlConnection con, SqlTransaction tran, ServerCharacterInfo ci)
        {
            return OnCharacterPersisting(con, tran, ci);
        }

        protected virtual bool OnCharacterDeleting(SqlConnection con, SqlTransaction tran, int characterId, ServerUser owner)
        {
            return true;
        }

        private bool CharacterUtil_OnCharacterDeleting(System.Data.SqlClient.SqlConnection con, System.Data.SqlClient.SqlTransaction tran, int characterId, ServerUser owner)
        {
            return OnCharacterDeleting(con, tran, characterId, owner);
        }

        /// <summary>
        /// Gets called when the login request has been resolved.  Return false to prevent login. Don't forget to call base.OnPlayerLoginResolved 
        /// if you override this method.
        /// </summary>
        /// <param name="login"></param>
        /// <param name="result"></param>
        protected override bool OnPlayerLoginResolved(PacketLoginRequest login, bool result, ref string msg)
        {
            if (base.OnPlayerLoginResolved(login, result, ref msg))
            {
                RegisterPacketHandler((int)PacketType.PacketGenericMessage, (int)GenericMessageType.RequestSelectCharacter, OnCharacterSelectRequest);
                RegisterPacketHandler((int)PacketType.PacketGenericMessage, (int)GenericMessageType.RequestDeleteCharacter, OnCharacterDeleteRequest);
                RegisterPacketHandler((int)PacketType.PacketGenericMessage, (int)GenericMessageType.RequestCreateCharacter, OnCharacterCreateRequest);
                RegisterPacketHandler((int)PacketType.PacketGenericMessage, (int)GenericMessageType.RequestCharacterListing, OnCharacterListingRequest);
            }
            return result;
        }

        private void OnCharacterListingRequest(INetworkConnection con, Packet msg)
        {
            // character was created successfully.  send a listing of all new characters
            PacketCharacterListing pcl = (PacketCharacterListing)con.CreatePacket((int)PacketType.CharacterListing, 0, false, false);
            List<ICharacterInfo> allToons = CharacterUtil.Instance.GetCharacterListing(this.ServerUser.ID);
            if (allToons.Count == 0)
            {
                if(ServerUser.CurrentCharacter != null)
                {
                    pcl.Characters.Add(ServerUser.CurrentCharacter.CharacterInfo);
                }
                else
                {
                    pcl.Characters = new List<ICharacterInfo>();
                }
            }
            else
            {
                foreach (ICharacterInfo inf in allToons)
                {
                    pcl.Characters.Add(inf);
                }
            }

            msg.ReplyPacket = pcl;      
        }

        private void OnCharacterCreateRequest(INetworkConnection con, Packet gmsg)
        {
            PacketGenericMessage msg = gmsg as PacketGenericMessage;
            string rsltMsg = "";
            ServerCharacterInfo ci = null;
            
            if(MyServer.UseCharacters)
            {
                ci = CharacterUtil.Instance.CreateNewCharacter(msg.Parms, ServerUser);
            }

            ReplyType rslt = ReplyType.Failure;
            if (ci != null && OnValidateCharacterCreateRequest(ci, ref rsltMsg))
            {
                if (CharacterUtil.Instance.PersistNewCharacter(ci, ServerUser, ref rsltMsg, !MyServer.RequireAuthentication))
                {
                    rslt = ReplyType.OK;
                }
            }

            PacketReply rep = CreateStandardReply(msg, rslt, rsltMsg);
            msg.ReplyPacket = rep;
        }        

        private void OnCharacterDeleteRequest(INetworkConnection con, Packet gmsg)
        {
            PacketGenericMessage msg = gmsg as PacketGenericMessage;
            string rsltMsg = "";
            ReplyType rslt = ReplyType.Failure;
            int characterId = msg.Parms.GetIntProperty((int)PropertyID.CharacterId).GetValueOrDefault();
           
            if(MyServer.UseCharacters && MyServer.RequireAuthentication && CharacterUtil.Instance.DeleteCharacter(characterId, ServerUser, false, "Player requested deletion from [" + con.RemoteEndPoint.ToString() + "].", ref rsltMsg))
            {
                rslt = ReplyType.OK;
            }

            PacketReply rep = CreateStandardReply(msg, rslt, rsltMsg);
            msg.ReplyPacket = rep;
        }

        private void OnCharacterSelectRequest(INetworkConnection con, Packet mesg)
        {
            PacketGenericMessage genMsg = mesg as PacketGenericMessage;
            string msg = "";
            ReplyType rslt = ReplyType.OK;
            int id = genMsg.Parms.GetIntProperty((int)PropertyID.CharacterId).GetValueOrDefault(-1);
            ServerCharacterInfo ci = null;
            
            // if we don't use characters, there wont be one in the DB
            if (MyServer.UseCharacters)
            {
                ci = CharacterUtil.Instance.LoadCharacter(ServerUser, id, ref msg);
            }

            if (ci == null && ServerUser.CurrentCharacter != null && ServerUser.CurrentCharacter.ID == id)
            {
                ci = ServerUser.CurrentCharacter;
            }

            PacketReply rep = CreateStandardReply(genMsg, rslt, msg);
            //genMsg.ReplyPacket = rep;

            if (ci == null)
            {
                genMsg.ReplyPacket = rep;
                rep.ReplyCode = ReplyType.Failure;
                rep.ReplyMessage = "Unable to load character.";
                return;
            }

            ServerUser.CurrentCharacter = ci;
            CharacterCache.CacheCharacter(ci, MyServer.ServerUserID, TimeSpan.MaxValue);

            rep.Parms.SetProperty((int)PropertyID.CharacterInfo, ci.CharacterInfo as IComponent);
            
            Send(rep); // reply needs to arrive before the OnSelected event is fired, in case OnSelected results in a server transfer
            OnCharacterSelected(ci);
        }

        protected virtual void OnCharacterSelected(ServerCharacterInfo toon)
        {
        }

        /// <summary>
        /// Gets called when the central server is requested, by an authorized source, to create a character.  Add any number of Int32, Int64, String or Float Properties
        /// to the property bag to have them persisted as part of the new character.  Return false (and probably populate @msg with a reason) to prevent character creation.
        /// After this method is called, all @character parts are checked against the server's Character template XML file.  If any of the properties 
        /// or stats on the character do not appear in the Character template file, character creation will fail.
        /// </summary>
        /// <param name="character">The character that will be created.  Modify these properties to your liking</param>
        /// <param name="msg">anything you want the player to know</param>
        /// <returns></returns>
        protected virtual bool OnValidateCharacterCreateRequest(ServerCharacterInfo character, ref string msg)
        {
            return true;
        }            

        /// <summary>
        /// Makes sure we have a proper character selected
        /// </summary>
        /// <returns></returns>
        protected bool ValidateHasCurrentCharacter()
        {
            if (ServerUser == null || ServerUser.CurrentCharacter == null)
            {
                return false;
            }

            return true;
        }

        protected override void OnConnectionReady()
        {
            base.OnConnectionReady();

            // Assign default character if necessary
            if (!MyServer.UseCharacters)
            {
                ServerCharacterInfo ci  = CharacterUtil.Instance.GetOrCreateDefaultCharacter(!MyServer.RequireAuthentication, ServerUser);
                if (ci == null)
                {
                    // disconnect user. couldn't get or create character.
                    Log1.Logger("Server.Character").Error("Internal server error.  Couldn't get or create default character for user " + ServerUser.AccountName);
                    KillConnection("Internal Server Error. Failed to get or create default character for user " + ServerUser.AccountName);
                    return;
                }

                ServerUser.CurrentCharacter = ci;
                CharacterCache.CacheCharacter(ci, MyServer.ServerUserID, TimeSpan.MaxValue);
            }

            ServerUser.OwningServer = MyServer.ServerUserID;
        }



    }
}
