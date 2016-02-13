using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading;

namespace Shared
{
    public abstract partial class Packet
    {
        ///// <summary>
        ///// This property is not serialized and is used only on the receiving end to store the packet processing result
        ///// for packets that NeedsReply.  This value is read in th NetworkConnection.OnAfterPacketProcessed method to send
        ///// the PacketReply to the sender.  Normally, when a packet is set to NeedsReply a generic PacketReply message is sent.
        ///// If this field is populated (ReplyPacket != null), however, the packet pointed to by this field is sent instead of a
        ///// generic PacketReply.  
        ///// <para>
        ///// Therefore, when processing a packet, first check to see if NeedsReply.  If NeedsReply, you
        ///// can either set the ReplyMessage and Reply in this header, or you can construct a more complex PacketReply and set it
        ///// in this field to have the networking API send the reply for you when processing has concluded.  If you want to 
        ///// completely take over sending a reply (in the event of a broadcast, perhaps) simply set NeedsReply to false in this 
        ///// header and the networking api will not send a reply in NetworkConnection.OnAfterPacketProcessed.
        ///// </para>
        ///// </summary>
        private PacketReply m_ReplyPacket = null;

        public PacketReply ReplyPacket
        {
            get { return m_ReplyPacket; }
            set 
            { 
                m_ReplyPacket = value;
                if (value != null)
                {
                    NeedsReply = true;
                    value.ReplyPacketID = PacketID;
                    PacketGenericMessage gen = this as PacketGenericMessage;
                    if (gen != null)
                    {
                        value.ReplyPacketSubType = gen.PacketSubTypeID;
                    }
                    value.ReplyPacketType = this.PacketTypeID;
                }
            }
        }
        

        /// <summary>
        /// The type of packet.  This is just an integer.  This means don't use overlapping PacketType IDs.
        /// For API implementation purposes, NEVER use a negative number.  API packets use negative numbers.
        /// For your own packets, use (unique) positive numbers and all will be well.  This member is automatically
        /// serialized when a Packet is sent.
        /// </summary>
        public int PacketTypeID
        {
            get { return m_PacketTypeID; }
            set 
            {
                m_PacketTypeID = value; 
            }
        }
        private int m_PacketTypeID;

        /// <summary>
        /// The sub type of a packet.  This is zero by default, meaning there is no sub type.
        /// </summary>
        public int PacketSubTypeID
        {
            get { return m_PacketSubTypeID; }
            set
            {
                m_PacketSubTypeID = value;
            }
        }
        private int m_PacketSubTypeID = 0;    

        /// <summary>
        /// The remote endpoint of where the packet came from.  This is not serialized - instead the value is populated when the packet is deserialized on the receiving end.
        /// </summary>
        public IPEndPoint RemoteEndPoint;

        /// <summary>
        /// Flags denoting encryption, compression and delivery type flags on the packet. This member is automatically
        /// serialized when a Packet is sent.
        /// </summary>
        public PacketFlags Flags;

        /// <summary>
        /// A packet ID that is unique within the origin.  In other words, any given machine will never generate packets with the same ID.
        /// These IDs are not globally unique.  In other words, two machines in a cluster could generate packets with the same ID.
        /// </summary>
        public int PacketID;

        private static int m_CurPacketId = 0;

        /// <summary>
        /// Gets the next available Packet ID.  Math overflow protection will turn the ID back over to zero.
        /// </summary>
        public static int NextPacketId
        {
            get
            {
                return Interlocked.Increment(ref m_CurPacketId);
            }
        }

        private bool IsFlagSet(PacketFlags flag)
        {
            return (this.Flags & flag) != 0;
        }

        private void SetFlag(PacketFlags flag)
        {
            this.Flags |= flag;
        }

        private void RemoveFlag(PacketFlags flag)
        {
            this.Flags &= ~flag;
        }

        /// <summary>
        /// True if the packet is encrypted
        /// </summary>
        public bool IsEncrypted
        {
            get
            {
                return IsFlagSet(PacketFlags.IsEncrypted);
            }
            set
            {
                if (value)
                {
                    SetFlag(PacketFlags.IsEncrypted);
                }
                else
                {
                    RemoveFlag(PacketFlags.IsEncrypted);
                }
            }
        }

        /// <summary>
        /// True, if the packet is compressed
        /// </summary>
        public bool IsCompressed
        {
            get
            {
                return IsFlagSet(PacketFlags.IsCompressed);
            }
            set
            {
                if (value)
                {
                    SetFlag(PacketFlags.IsCompressed);
                }
                else
                {
                    RemoveFlag(PacketFlags.IsCompressed);
                }
            }
        }

        /// <summary>
        /// True, if the packet requested delivery acknoledgement
        /// </summary>
        public bool NeedsDeliveryAck
        {
            get
            {
                return IsFlagSet(PacketFlags.NeedsDeliveryAck);
            }
            set
            {
                if (value)
                {
                    SetFlag(PacketFlags.NeedsDeliveryAck);
                }
                else
                {
                    RemoveFlag(PacketFlags.NeedsDeliveryAck);
                }
            }
        }

        /// <summary>
        /// True, if the packet requested a reply packet after processing 
        /// to indicate the outcome of the packet to the original sender
        /// </summary>
        public bool NeedsReply
        {
            get
            {
                return IsFlagSet(PacketFlags.NeedsReply);
            }
            set
            {
                if (value)
                {
                    SetFlag(PacketFlags.NeedsReply);
                }
                else
                {
                    RemoveFlag(PacketFlags.NeedsReply);
                }
            }
        }

        /// <summary>
        /// Critical packets will cause the connection to be dropped if the connection can't SEND them fast enough or they are not processed
        /// successfully.  They are deemed critical for gameplay.
        /// Movement packets might not be critical, attack packets might be - it depends on your application.
        /// </summary>
        public bool IsCritical
        {
            get
            {
                return IsFlagSet(PacketFlags.IsCritical);
            }
            set
            {
                if (value)
                {
                    SetFlag(PacketFlags.IsCritical);
                }
                else
                {
                    RemoveFlag(PacketFlags.IsCritical);
                }
            }
        }

        /// <summary>
        /// Packets are sent via TCP unless this flag is set. UDP packets are sent faster, but have several limitations: They may arrive more than one time, They may
        /// arrive out of order, They may never arrive.  If you need any of those things, send the packet via TCP which has built in functionality for all of these.
        /// You may set the UDP flag and the NeedsDeliveryAck flag at the same time.  Wisp connections will only process a UDP packet if it is older than the last one
        /// it processed (based on Packet sequence IDs, which are automatically generated), so out of order and multiple receive UDP packets are less of a problem.
        /// <para>If you can't live with the limitations of UDP but are finding that your TCP connections are too slow (even on high bandwidth lines), try turning off Packet "Nageling" with the App.Config
        /// option "DisableTCPDelay" = TRUE, which will turn off the TCP algorithm that is partly responsible for the throttling behavior in high packet loss scenarios.</para>
        /// </summary>
        public bool IsUDP
        {
            get
            {
                return IsFlagSet(PacketFlags.UDP);
            }
            set
            {
                if (value)
                {
                    SetFlag(PacketFlags.UDP);
                }
                else
                {
                    RemoveFlag(PacketFlags.UDP);
                }
            }
        }
    }
}
