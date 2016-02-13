using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared
{
    /// <summary>
    /// Lobby game based character extensions
    /// </summary>
    public static class ServerCharacterExtensions
    {
        /// <summary>
        /// Returns a reference to the game that the character is attached to
        /// </summary>
        /// <param name="toon"></param>
        /// <returns></returns>
        public static GameServerGame GetCurrentGame(this ServerCharacterInfo toon)
        {
            GameServerGame g = toon.Properties.GetComponentProperty("CurrentGame") as GameServerGame;
            return g;
        }

        /// <summary>
        /// Sets a reference to the game that the character is currently attached to
        /// </summary>
        /// <param name="toon"></param>
        /// <param name="curGame"></param>
        public static void SetCurrentGame(this ServerCharacterInfo toon, GameServerGame curGame)
        {
            toon.Properties.SetProperty("CurrentGame", curGame);
            toon.Properties.SetLocalFlag("CurrentGame", true);
        }
    }
}
