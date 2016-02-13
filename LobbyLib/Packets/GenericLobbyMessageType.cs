public enum GenericLobbyMessageType
{
    /// <summary>
    /// Request for a full listing of all matches/rooms/instances that we know about
    /// </summary>
    RequestGameListing = 20,

    /// <summary>
    /// Request to create a new game
    /// </summary>
    CreateGame = 21,

    /// <summary>
    /// A server, who's games we're taking, has gone offline.  we should no longer advertise those games.
    /// if that server comes back online, we'll get a fresh game listing from that server at that time
    /// </summary>
    SubServerWentOffline = 22,

    /// <summary>
    /// Player is leaving a game/match/room
    /// </summary>
    LeaveGame = 23,

    /// <summary>
    /// Player is joining a game/match/room
    /// </summary>
    JoinGame = 24,

    /// <summary>
    /// Player wants to kick off starting a game.
    /// </summary>
    RequestStartGame = 25,

    /// <summary>
    /// Player wants to find a quick match
    /// </summary>
    RequestQuickMatch = 26
}