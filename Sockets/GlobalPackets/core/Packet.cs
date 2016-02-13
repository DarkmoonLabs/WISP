using System;
using System.Collections;
using System.Text;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

namespace Shared
{
	/// <summary>
	/// One network packet.
	/// </summary>
	public abstract partial class Packet
	{
        /// <summary>
        /// If you routinely send packets larger than the starting buffer size, it's possible that memory will be "held" by the GC due to 
        /// array resizing, which may cause the app to appear like it's leaking memory.  Memory will eventually be released when memory pressure mounts,
        /// but you can mitigate this by having a large enough buffer size.
        /// </summary>
        public static int SERIALIZE_BUFFER_SIZE = 512;
        /// <summary>
        /// To be used by inheriting packets to serialize their data for transmission
        /// </summary>
        protected byte[] m_SerializeBuffer = new byte[SERIALIZE_BUFFER_SIZE];

        /// <summary>
        /// When the packet was sent
        /// </summary>
		public long SentOnUTC { get; set; }        

		/*/ ENVELOPE (never encrypted) - always 10 bytes long
		/// =========================	
        /// Int64 message length
		/// BOOL IsHeaderEncrypted
		/// byte HeaderLength
		///
		/// HEADER (if encrypted, then it's always encrypted with the "connection" key,)
		/// ==========
		/// Int64 BodyLength (Body) in bytes - anything longer than Int32.MaxValue will need to be sent via stream
		/// Int32 PacketType
		/// BYTE PacketFlags (IsBodyEncrypted, IsP2PTransmission, IsCompressed)
		/// string EndpointID
		
		/// BODY (if encrypted, then it's always encrypted with the EndpointID's key)
		/// ==========
		// byte[] data */

        /// <summary>
        /// Returns a binary representation of this packet suitable for transmission across the wire.  Override this method to control the 
        /// serialization process.  The byte array returned by this function is likely bigger than the actual data required.  Read Pointer.Position
        /// after the operation to see where exactly the data ends
        /// </summary>
        /// <param name="p">the pointer indicating where in the return array the actual data stops.  We use this to keep from having to copy the data
        /// into a perfectly sized array.</param>
        /// <returns>a binary representation of the packet</returns>
		public virtual byte[] Serialize(Pointer p)
		{
            BitPacker.AddInt(ref m_SerializeBuffer, p, PacketID);
            BitPacker.AddLong(ref m_SerializeBuffer, p, SentOnUTC);
            return m_SerializeBuffer;
		}

		/// <summary>
		/// Takes raw data and constructs members from it.
		/// Override this method to reconstitute the packet object from raw data in the form of the ArrayList argument
		/// </summary>
		/// <returns>failure or success</returns>
        public virtual bool DeSerialize(byte[] dat, Pointer p)
		{
            PacketID = BitPacker.GetInt(dat, p);
            SentOnUTC = BitPacker.GetLong(dat, p);
			return false;
		}
        
    }
}

