using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared
{
    /// <summary>
    /// Default game abort strategy.  Aborts the game if zero players are remaining.
    /// </summary>
    public class GameAbortStrategy : IGameAbortStrategy
    {

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

        public virtual void NotifyPlayerAddedToGame(ICharacterInfo toon)
        {
            
        }

        public virtual void NotifyPlayerLeftGame(int toon)
        {
            if (OwningGame == null)
            {
                return;
            }

            if (OwningGame.Players.Count < 1) // last player has left
            {                
                OwningGame.EndGame("Game [" + OwningGame.Name + "] [" + OwningGame.GameID.ToString() + "] has no players left. Removing...");                
            }
        }


    }
}
