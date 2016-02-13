using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared
{
    /// <summary>
    /// Default game start strategy. DoesGameMeetStartConditions returns true if MinPlayersToStartGame is met.
    /// </summary>
    public class GameStartStrategy : IGameStartStrategy
    {
        public GameStartStrategy()
        {
            CanGameBeStartedManually = true;
            MinPlayersToStartGame = 1;
        }

        public GameServerGame OwningGame
        {
            get
            {
                return m_OwningGame;
            }
            set
            {
                m_OwningGame = value;
            }
        }
        private GameServerGame m_OwningGame;

        public bool CanGameBeStartedManually
        {
            get 
            { 
                return m_CanGameBeStartedManually;
            }
            set
            {
                m_CanGameBeStartedManually = value;
            }
        }
        private bool m_CanGameBeStartedManually;

        public int MinPlayersToStartGame { get; set; }

        public bool DoesGameMeetStartConditions
        {
            get 
            {
                try
                {
                    if (OwningGame.Players.Count > MinPlayersToStartGame)
                    {
                        return true;
                    }
                }
                catch
                {
                    return false;
                }

                return false;
            }
        }

        public virtual void NotifyPlayerAddedToLobby(ICharacterInfo toon)
        {
            try
            {
            }
            catch { }
        }

        public virtual void NotifyPlayerLeftLobby(int toon)
        {
            try
            {
                if (OwningGame.Players.Count < 1)
                {
                    OwningGame.EndGame("Game [" + OwningGame.Name + "] [" + OwningGame.GameID.ToString() + "] has no players left. Removing...");      
                }
            }
            catch { }
        }

    }
}
