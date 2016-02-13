using System;
namespace Shared
{
    /// <summary>
    /// Interface that all outgoing (i.e. client) connections must implement.
    /// </summary>
    public interface IClientConnection
    {
        /// <summary>
        /// The account name that this client is logging in with.
        /// </summary>
        string AccountName { get; set; }

        /// <summary>
        /// Starts the connection process.
        /// </summary>
        /// <param name="serverAddress">Address, IP or otherwise, to connect to</param>
        /// <param name="port">Remote port to connect to</param>
        /// <param name="username">the username to connect with. gets stored in AccountName</param>
        /// <param name="password">the password to conenct with.  gets stored in Password</param>
        void BeginConnect(string serverAddress, int port, string username, string password);
        void BeginConnect(string serverAddress, int port, string username, string password, PacketLoginRequest.ConnectionType connectionType);
       /// <summary>
       /// Network clocks synchronize automatically after a few seconds of connectivity. It's not a real time, but rather a time that is as close as possible to the same
       /// between the client object and server object.
       /// </summary>
        NetworkClock Clock { get; }
        /// <summary>
        /// Is the client attempting to connect, but has not actually yet connected.
        /// </summary>
        bool ConnectionInProgress { get; }
        /// <summary>
        /// Is this connection the result of a server transfer or a direct connection attempt.
        /// </summary>
        PacketLoginRequest.ConnectionType ConnectionType { get; }
        /// <summary>
        /// Will be true, if we ever tried to make a connection, regardless of outcome.
        /// </summary>
        bool ConnectionWasInitiated { get; }
        /// <summary>
        /// True if the connection has been terminated.  Sockets are never re-used.
        /// </summary>
        bool ConnectionWasKilled { get; }
#if !SILVERLIGHT
        /// <summary>
        /// Should NAT punchthrough be maintained for UDP connections.
        /// </summary>
        bool EnableUDPKeepAlive { get; set; } 
        /// <summary>
        /// Sends a NAT punchthrough packet to ensure the hole stays open.
        /// </summary>
        /// <param name="thisListenOn">the port you're listening on for the return packet</param>
        /// <returns></returns>
        bool SendUDPPoke(int thisListenOn);
#endif
        /// <summary>
        /// After we log in successfully, we will have a list of User Roles that we qualify for.  This lets us check to see if we're in specific role.
        /// </summary>
        /// <param name="role"></param>
        /// <returns></returns>
        bool IsInRole(string role);
        /// <summary>
        /// Set to true if you want to connect and create a new account on the fly, using AccountName and Password as the credentials for the new account.
        /// </summary>
        bool IsNewAccount { get; set; }
        /// <summary>
        /// Are we logged in and ready to conduct business, i.e. have our credentials been accepted?
        /// </summary>
        bool LoggedIn { get; set; }
        /// <summary>
        /// The password used to log in with.
        /// </summary>
        string Password { get; set; }
        /// <summary>
        /// If ProcessPacketsImmediately == false, you must call this method to process all queued networking packets.
        /// No packets are ACKnowledged or replied to until this method is called. Calling this method clears out the inbox.
        /// </summary>
        /// <returns>the number of packets processed</returns>
        int ProcessNetworking();

        /// <summary>
        /// The roles the server has us down for.  Populated after logging in.
        /// </summary>
        string[] Roles { get; set; }
       
        /// <summary>
        /// After we try to log in, this event lets us know what the result was.
        /// </summary>
        event ServerLoginResultArrivedDelegate ServerLoginResultArrived;

        /// <summary>
        /// The server we're currently on, is requesting us to transfer to another server.  This event gives us the specific data for the transfer.
        /// </summary>
        event ServerTransferDirectiveDelegate ServerTransferDirective;

        /// <summary>
        /// When we try to make a TCP connection, this event lets us know what the result of the connection was (this is separate from the login attempt, which occurs AFTER the actual TCP 
        /// connection is made).
        /// </summary>
        event Action<IClientConnection, bool, string> SocketConnectionConcluded;

        /// <summary>
        /// Fires after the encryption RSA/Rijndael handshake happens.  Once this fires, you can send and receive encrypted packets by setting the Encrypted flag on the packet to true.
        /// </summary>
        event EventHandler SocketSecured;

    }
}
