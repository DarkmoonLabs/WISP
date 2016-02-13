using System;
using System.Collections.Generic;
using System.Text;

namespace Shared
{
    /// <summary>
    /// From the client to the login server, requesting handoff to a particular server cluster
    /// </summary>
    public class PacketRequestHandoffToServerCluster : Packet
    {
        public PacketRequestHandoffToServerCluster()
            : base()
        {
            m_TargetResource = Guid.Empty;         
        }

        public string TargetServerName { get; set; }

        private Guid m_TargetResource;
        /// <summary>
        /// The target resource that we are connecting for, if any
        /// </summary>
        public Guid TargetResource
        {
            get { return m_TargetResource; }
            set { m_TargetResource = value; }
        }


        public override bool DeSerialize(byte[] data, Pointer p)
        {
            base.DeSerialize(data, p);
            TargetServerName = BitPacker.GetString(data, p);
            TargetResource = new Guid(BitPacker.GetString(data, p));
            return true;
        }

        public override byte[] Serialize(Pointer p)
        {
            base.Serialize(p);
            BitPacker.AddString(ref m_SerializeBuffer, p, TargetServerName);
            BitPacker.AddString(ref m_SerializeBuffer, p, TargetResource.ToString());

            return m_SerializeBuffer;
        }
    }
}
