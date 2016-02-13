using System;

public enum ServerPacketType : int
{
    /// <summary>
    /// A server has consented to our transferring a player to it
    /// </summary>
    PacketPlayerAuthorizedForTransfer = -2000,       

    /// <summary>
    /// A server is requesting that a player be transferred to another server
    /// </summary>
    RequestPlayerHandoff = -1999,

    /// <summary>
    /// A server Heartbeat tick
    /// </summary>
    ServerStatusUpdate = -1998,

    /// <summary>
    /// A list of all characters a server is hosting
    /// </summary>
    CharacterListing = -1997,

    // Generic relay to a user on a remote server
    Relay = -1996
}