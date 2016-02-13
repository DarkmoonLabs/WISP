using System;
using System.Collections.Generic;
using System.Text;

namespace Shared
{
    /// <summary>
    /// Packet from cluster server A to cluster server B, asking that cluster server B accept a player transfer on behalf of cluster server A
    /// <para>
    /// Results in a PacketPlayerAuthorizedForTransfer reply
    /// </para>
    /// </summary>
    public class PacketRelayPlayerHandoffRequest : Packet
    {
        public PacketRelayPlayerHandoffRequest()
            : base()
        {
            m_TargetResource = Guid.Empty;
            Profile = new AccountProfile("-");
            OwningServer = "";
        }

        /// <summary>
        /// The player, i.e. system user, that wants to be transferred
        /// </summary>
        public Guid Player { get; set; }
        public Guid SharedSecret { get; set; }
        /// <summary>
        /// When a player gets transferred around, one server in the cluster is usually designated as the "owner" and keeps track of where the character
        /// currently is.
        /// </summary>
        public string OwningServer { get; set; }
        /// <summary>
        /// If a character is to be transferred as part of the transfer request, the character will be stored here.
        /// This property may be null if it's a generic connection transfer request, that's not transferring  a character.
        /// </summary>
        public ServerCharacterInfo Character { get; set; }

        /// <summary>
        /// The account name of the Player in question
        /// </summary>
        public string AccountName { get; set; }

        /// <summary>
        /// Any sort of account profile variables that we need to share.  The sort of thing that's connected with the account, not any specific character.
        /// For instance, the max number of character slots, or content access permissions, etc could be sent here.
        /// </summary>
        public AccountProfile Profile { get; set; }

        private Guid m_TargetResource;
        /// <summary>
        ///  If we're connecting for a specific resource that we want to access on that server, enter it here.
        /// </summary>
        public Guid TargetResource
        {
            get { return m_TargetResource; }
            set { m_TargetResource = value; }
        }

        public override bool DeSerialize(byte[] data, Pointer p)
        {
            base.DeSerialize(data, p);
            Player = new Guid(BitPacker.GetString(data, p));
            SharedSecret = new Guid(BitPacker.GetString(data, p));
            AccountName = BitPacker.GetString(data, p);
            TargetResource = new Guid(BitPacker.GetString(data, p));
            Profile = (AccountProfile) BitPacker.GetSerializableWispObject(data, p);
            bool haveCharacterData = BitPacker.GetBool(data, p);
            if (haveCharacterData)
            {
                Character = BitPacker.GetComponent(data, p) as ServerCharacterInfo;
            }
            OwningServer = BitPacker.GetString(data, p);
            return true;
        }

        public override byte[] Serialize(Pointer p)
        {
            base.Serialize(p);
            BitPacker.AddString(ref m_SerializeBuffer, p, Player.ToString());
            BitPacker.AddString(ref m_SerializeBuffer, p, SharedSecret.ToString());
            BitPacker.AddString(ref m_SerializeBuffer, p, AccountName);
            BitPacker.AddString(ref m_SerializeBuffer, p, TargetResource.ToString());
            BitPacker.AddSerializableWispObject(ref m_SerializeBuffer, p, Profile);
            BitPacker.AddBool(ref m_SerializeBuffer, p, Character != null);
            if (Character != null)
            {
                BitPacker.AddComponent(ref m_SerializeBuffer, p, Character);
            }
            BitPacker.AddString(ref m_SerializeBuffer, p, OwningServer);
            return m_SerializeBuffer;
        }
        
    }
}
