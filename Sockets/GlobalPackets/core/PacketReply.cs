using System;
using System.Collections.Generic;
using System.Text;

namespace Shared
{
    public enum ReplyType : int
    {
        /// <summary>
        /// Packet received, but client wasn't authorized. Get a new Auth ticket and resend.
        /// </summary>
        AuthorizationTicketExpired = -1,

        /// <summary>
        /// Not sent yet
        /// </summary>
        None = 0,

        /// <summary>
        /// Packet processed without errors
        /// </summary>
        OK = 1,

        /// <summary>
        /// Sent and waiting for acknowledgement
        /// </summary>
        Waiting = 2,

        /// <summary>
        /// Command rejected by recipent
        /// </summary>
        Failure = 3
    }

    public class PacketReply : Packet
    {
        public PacketReply()
        {
            ReplyCode = ReplyType.None;
            ReplyMessage = string.Empty;
            ReplyPacketType = (int)PacketType.Null;
            ReplyPacketSubType = -1;
            ReplyPacketID = -1;
            Parms = new PropertyBag();
        }

        public ReplyType ReplyCode { get; set; }
        
        /// <summary>
        /// The packetID to which we are replying
        /// </summary>
        public int ReplyPacketID { get; set; }

        /// <summary>
        /// The type of the packet that generated this reply
        /// </summary>
        public int ReplyPacketType { get; set; }

        /// <summary>
        /// The sub-type of the packet that generated this reply.  Mostly used for replies to PacketGenericMessage, where OriginPacketSubType will be equal to the GenericMessageType
        /// </summary>
        public int ReplyPacketSubType { get; set; }

        /// <summary>
        /// Random parameters
        /// </summary>
        public PropertyBag Parms { get; set; }

        /// <summary>
        /// A textual message, if any
        /// </summary>
        public string ReplyMessage { get; set; }

        public override bool DeSerialize(byte[] data, Pointer p)
        {
            base.DeSerialize(data, p);

            ReplyCode = (ReplyType)BitPacker.GetInt(data, p);
            ReplyPacketID = BitPacker.GetInt(data, p);
            ReplyMessage = BitPacker.GetString(data, p);
            ReplyPacketType = BitPacker.GetInt(data, p);
            ReplyPacketSubType = BitPacker.GetInt(data, p);

            bool hasProps = BitPacker.GetBool(data, p);
            if (hasProps)
            {
                Parms = BitPacker.GetPropertyBag(data, p);
            }
            return true;
        }

        public override byte[] Serialize(Pointer p)
        {
            base.Serialize(p);

            BitPacker.AddInt(ref m_SerializeBuffer, p, (int)ReplyCode);
            BitPacker.AddInt(ref m_SerializeBuffer, p, ReplyPacketID);
            BitPacker.AddString(ref m_SerializeBuffer, p, ReplyMessage);
            BitPacker.AddInt(ref m_SerializeBuffer, p, ReplyPacketType);
            BitPacker.AddInt(ref m_SerializeBuffer, p, ReplyPacketSubType);

            bool hasParms = Parms.PropertyCount > 0;
            BitPacker.AddBool(ref m_SerializeBuffer, p, hasParms);
            if (hasParms)
            {
                BitPacker.AddPropertyBag(ref m_SerializeBuffer, p, Parms);
            }
            return m_SerializeBuffer;
        }


    }
}
