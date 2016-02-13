using System;

namespace Shared
{
    /// <summary>
    /// System packet types are reserved -1000 to -1, inclusive
    /// </summary>
    public enum PacketType : int
    {
        PacketRijndaelExchangeRequest      = -1000,
        RijndaelExchange    = -999,
        LineSecured         = -998,
        LoginRequest        = -997,
        LoginResult         = -996,
        Null = -995,
        GenericReply = -994,
        
        PacketGenericMessage = -993,
        PacketStream = -992,
        
        /// <summary>
        /// A player has requested connection to a specific server cluster, i.e. they want to play on server X.
        /// </summary>
        RequestHandoffToServer = -991,

        /// <summary>
        /// The login server is sending the client a message, letting him know that the server cluster the player has
        /// selected has accepted the request for connection from the player. This packet includes the necessary
        /// addresses, keys, tickets, etc to allow the client to connect to the target cluster.
        /// </summary>
        PacketGameServerAccessGranted = -990,

        /// <summary>
        /// A listing of a user's characters
        /// </summary>
        CharacterListing = -989,

        /// <summary>
        /// Packet delivery acknoledgement
        /// </summary>
        ACK = -988,

        /// <summary>
        /// Lets the recipient know what port we're listening on for UDP
        /// </summary>
        NATInfo = -987,

        /// <summary>
        /// Transmit clock synchronization information 
        /// </summary>
        ClockSync = -986,

        /// <summary>
        /// Client attempted to send a packet that required authentication, but that client was not authorized (either never, or not any more).
        /// </summary>
        NotAuthorized = -985

    }
}
