using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared
{
    public class LobbyClientTB : LobbyClient
    {
        private static LobbyClientTB m_Instance;
        /// <summary>
        /// Singleton instance.
        /// </summary>
        public static LobbyClientTB Instance
        {
            get
            {
                if (m_Instance == null)
                {                    
                    m_Instance = new LobbyClientTB();
                   // Log.LogMsg("*** CREATING NEW LOBBYCLIENTTB OBJECT " + m_Instance.GetHashCode().ToString() + " ***");
                }

                //Log.LogMsg("*** RETURNING LOBBYCLIENTTB OBJECT " + m_Instance.GetHashCode().ToString() + " ***");
                return m_Instance;
            }
        }

        public LobbyClientTB()
            : base()
        {
            Factory.Instance.Register(typeof(BeginningOfTurnPhase), delegate { return new BeginningOfTurnPhase(0); });
            Factory.Instance.Register(typeof(EndOfTurnPhase), delegate { return new EndOfTurnPhase(0); });
            Factory.Instance.Register(typeof(MainPhase), delegate { return new MainPhase(0); });
            Factory.Instance.Register(typeof(RoundEndSequenceItem), delegate { return new RoundEndSequenceItem(0); });
            Factory.Instance.Register(typeof(RoundStartupSequenceItem), delegate { return new RoundStartupSequenceItem(0); });
        }

        protected override ClientGame OnCreateCurrentGameObject(Game baseData)
        {
            return new ClientGameTB(baseData, this, m_GameServer);
        }

        protected override ClientGameServerOutboundConnection OnGameServerConnectionCreate(bool isBlocking)
        {
            LobbyClientGameServerOutboundConnectionTB con = new LobbyClientGameServerOutboundConnectionTB(isBlocking);
            return con;
        }


    }
}
