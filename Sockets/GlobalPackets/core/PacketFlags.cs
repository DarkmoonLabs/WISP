using System;

namespace Shared
{
    /// <summary>
    /// Bit field, containing various flags about the packet state
    /// </summary>
	[Flags]
	public enum PacketFlags : byte
	{
		None = 0,
		IsEncrypted = 1, 
		IsCompressed = 2,

        /// <summary>
        /// If the sender would like a message indicating that we have receive the packet, this is true.  NetworkConnection.OnBeforePacketProcessed
        /// is responsible for sending these acknolowdgement reply packets.  Note that an acknowledgement says nothing of the processing result for that
        /// packet, just that we got the packet.
        /// </summary>
        NeedsDeliveryAck = 4,

        /// <summary>
        /// If true, the receiving party should reply with an acknowledgement to this packet. If this packet should generate a failure of any kind on the receiving end,
        /// a failure reply should be sent, regardless of of the value of this property.
        /// </summary>
        NeedsReply = 8,

        /// <summary>
        /// Critical packets will cause the connection to be dropped if the connection can't SEND them fast enough or they are not processed
        /// successfully.  They are deemed critical for gameplay. This is option is ignored for UDP packets.
        /// Movement packets might not be critical, attack packets might be - it depends on your application.
        /// </summary>
        IsCritical = 16,

        /// <summary>
        /// Packets are sent via TCP unless this flag is set. UDP packets are sent faster, but have several limitations: They may arrive more than one time, They may
        /// arrive out of order, They may never arrive.  If you need any of those things, send the packet via TCP which has built in functionality for all of these.
        /// You may set the UDP flag and the NeedsDeliveryAck flag at the same time.  Wisp connections will only process a UDP packet if it is older than the last one
        /// it processed (based on Packet sequence IDs, which are automatically generated), so out of order and multiple receive UDP packets are less or a problem.
        /// <para>If you can't live with the limitations of UDP but are finding that your TCP connections are too slow, try turning off Packet "Nageling" with the App.Config
        /// option "DisableTCPDelay" = TRUE, which will turn off the TCP algorithm that is partly responsible for this throttling behavior in high packet loss scenarios.</para>
        /// </summary>
        UDP = 32,     
	}
}
