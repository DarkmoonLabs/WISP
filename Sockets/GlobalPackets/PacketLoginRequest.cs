using System;
using System.Collections.Generic;
using System.Text;

namespace Shared
{
    /// <summary>
    /// Encapsulates one login request
    /// <para>
    /// Results in a PacketLoginResult reply
    /// </para>
    /// </summary>
    public class PacketLoginRequest : Packet
    {
        public PacketLoginRequest()
            : base()
        {
            Parms = new PropertyBag();
            LoginConnectionType = ConnectionType.DirectConnect;
        }

        /// <summary>
        /// Misc paramters to pass for login.
        /// </summary>
        public PropertyBag Parms { get; set; }

        private string m_AccountName = "";
        public string AccountName
        {
            get
            {
                return m_AccountName;
            }
            set
            {
                m_AccountName = value;
            }
        }

        private string m_Password = "";
        public string Password
        {
            get
            {
                return m_Password;
            }
            set
            {
                m_Password = value;
            }
        }

        public bool IsNewAccount { get; set; }

        /// <summary>
        /// Describes how the client is connecting.  Determines how the login request is validated.
        /// </summary>
        public enum ConnectionType : int
        {
            /// <summary>
            /// Connecting directly to the server.
            /// </summary>
            DirectConnect = 1,
            /// <summary>
            /// Assisted transfer is a transfer is one where the transferring server has directly notified the target server that the client is transferring.  
            /// </summary>
            AssistedTransfer = 2,
            /// <summary>
            /// Unassisted transfer is a transfer where the transferring server told the client to transfer to another server and did not notify the receiving server, but instead 
            /// generated an AUTH ticket for the client and stored it in the central Session DB.  The receiving server will check the AUTH ticket against the Session DB when the client 
            /// arrived.
            /// </summary>
            UnassistedTransfer = 3
        }

        public ConnectionType LoginConnectionType { get; set; }

        public override bool DeSerialize(byte[] data, Pointer p)
        {
            base.DeSerialize(data, p);
            LoginConnectionType = (ConnectionType)BitPacker.GetInt(data, p);
            m_AccountName = BitPacker.GetString(data, p);// System.Text.Encoding.UTF8.GetString(data, 4, userLen);
            m_Password = BitPacker.GetString(data, p); // System.Text.Encoding.UTF8.GetString(data, 8 + userLen, passLen);
            IsNewAccount = BitPacker.GetBool(data, p);
            Parms = BitPacker.GetPropertyBag(data, p);
            return true;
        }

        public override byte[] Serialize(Pointer p)
        {
            base.Serialize(p);
            BitPacker.AddInt(ref m_SerializeBuffer, p, (int)LoginConnectionType);
            BitPacker.AddString(ref m_SerializeBuffer, p, AccountName);
            BitPacker.AddString(ref m_SerializeBuffer, p, Password);
            BitPacker.AddBool(ref m_SerializeBuffer, p, IsNewAccount);
            BitPacker.AddPropertyBag(ref m_SerializeBuffer, p, Parms);
            return m_SerializeBuffer;
        }
    }
}
