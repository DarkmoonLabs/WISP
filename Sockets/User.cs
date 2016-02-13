using System;
using System.Collections.Generic;
using System.Text;

namespace Shared
{
    /// <summary>
    /// Data class which stores basic user information such as account name, authorization ticket for server access, connection encryption key, etc
    /// </summary>
    public class User
    {
        public User()
        {
            IsAuthenticated = false;
            LoginFailed = false;
            AuthTicket = Guid.Empty;
            ID = Guid.Empty;
            SharedRijndaelKey = new byte[0];
        }

        private string m_AccountName;

        /// <summary>
        /// The account/login name for this user
        /// </summary>
        public string AccountName
        {
            get { return m_AccountName; }
            set { m_AccountName = value; }
        }
        
        /// <summary>
        /// Is the user authenticated by the login server? or was the account info rejected? or possibly we never tried to login yet.
        /// </summary>
        public bool IsAuthenticated { get; set; }

        /// <summary>
        /// Login was attempted, and failed?
        /// </summary>
        public bool LoginFailed { get; set; }

        /// <summary>
        /// The authentication ticket that we got from the login server, on behalf of the target server cluster. Server clusters do not know anything
        /// about the user database, instead they rely on the login server to authenticate users.  When users become authenticated and are handed off 
        /// to a target server cluster, that cluster is given a copy of this ticket by the login server.  The user will present this ticket to the 
        /// target cluster when attempting to access any of the cluster's resources. Authentication tickets will eventually expire, depending on
        /// server settings.
        /// </summary>
        public Guid AuthTicket { get; set; }

        /// <summary>
        /// The User ID in the database for this account
        /// </summary>
        public Guid ID { get; set; }

        /// <summary>
        /// Shared rijndael key with the server.  This is used to encrypt / decrypt the network communication.
        /// </summary>
        public byte[] SharedRijndaelKey { get; set; }

    }
}
