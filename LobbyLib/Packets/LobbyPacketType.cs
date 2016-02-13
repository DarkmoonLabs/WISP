using System;

namespace Shared
{
    public enum LobbyPacketType : int
    {
        /// <summary>
        /// There was a change to  game/room/match. A player was added/removed or the game itself was added/removed
        /// </summary>
        MatchNotification = 3000,

        /// <summary>
        /// A server is sending us a complete list of all matches that they know about
        /// </summary>
        MatchRefresh = 3001,

        /// <summary>
        /// A packet containing a communication for a specific game or instance
        /// </summary>
        GameMessage = 3003,

        /// <summary>
        /// Contains info about potential quick match results
        /// </summary>
        QuickMatchResult = 3004

    }
}
