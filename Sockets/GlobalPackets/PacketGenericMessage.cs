using System;
using System.Collections.Generic;
using System.Text;

namespace Shared
{
    /// <summary>
    /// Various packet types must be identified by a MessageType digit.  This enum is used internally by WISP.
    /// </summary>
    public enum GenericMessageType : byte
    {
        GetLatestVersion = 1,
        VersionIsCurrent = 2,
        Ping = 3,
        RequestCharacterListing = 4,
        Pong = 5,
        CharacterTransferComplete = 6,
        SubServerWentOffline = 7,
        RequestCreateCharacter = 8,
        RequestSelectCharacter = 9,
        CharacterActivated = 10,
        RequestDeleteCharacter = 11,
        CharacterDisconnected = 12,
        Notes = 13,
        ServersOnMachine = 14,
        ServiceInstallResult = 15
    }

    /// <summary>
    /// A generic message that passes between the client and the server (and vice versa).  Negative MessageType IDs are reserved by the API.
    /// Use any other number for your own purposes.
    /// </summary>
    public class PacketGenericMessage : Packet
    {
        public PacketGenericMessage()
            : base()
        {
            TextMessage = string.Empty;
            Parms = new PropertyBag();
        }

        /// <summary>
        /// Extra text data to send
        /// </summary>
        public string TextMessage { get; set; }

        ///// <summary>
        ///// The parameters for the command
        ///// </summary>
        //public Dictionary<string, string> Parms { get; set; }
        public PropertyBag Parms { get; set; }

        public override bool DeSerialize(byte[] data, Pointer p)
        {
            base.DeSerialize(data, p);
            TextMessage = BitPacker.GetString(data, p);
            Parms = BitPacker.GetPropertyBag(data, p);

            return true;
        }

        public override byte[] Serialize(Pointer p)
        {
            base.Serialize(p);
            BitPacker.AddString(ref m_SerializeBuffer, p, TextMessage);
            BitPacker.AddPropertyBag(ref m_SerializeBuffer, p, Parms);
            return m_SerializeBuffer;
        }

    }
}
