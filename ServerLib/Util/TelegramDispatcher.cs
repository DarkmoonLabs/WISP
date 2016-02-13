using System;
using System.Collections.Generic;
using System.Text;
using Shared;

namespace GameLib
{
    /// <summary>
    /// Sends telegrams to game objects
    /// </summary>
    public class TelegramDispatcher
    {
        /// <summary>
        /// Call this each time the game loop iterates.  This method handles sending out delayed telegrams
        /// </summary>
        public void DispatchDelayedTelegrams()
        {
            DateTime now = DateTime.Now;
            while(m_TelegramQ.Count > 0)
            {
                if (m_TelegramQ.Keys[0] <= now)
                {
                    // Grab the telegram and remove it from the queue
                    Telegram t = m_TelegramQ.Values[0];
                    m_TelegramQ.RemoveAt(0);

                    // Send it to the object in question
                    IGameObject go = m_ActorManager.GetGameObjectFromId(t.Recipient) ;
                    
                    if(go == null || (!(go is IMessagable)))
                    {
                        // if the recipient doesn't exist anymore/yet the message gets lost. 
                        // sorry it didn't work out.
                        continue;
                    }

                    ((IMessagable)go).HandleTelegram(t);
                }

                break;
            }

        }

        private ServerGameObjectManager m_ActorManager = null;

        private TelegramDispatcher() { }
        public TelegramDispatcher(ServerGameObjectManager gamesActorManager) 
        {
            m_ActorManager = gamesActorManager;
        }

        /*
        /// <summary>
        /// Singleton instance
        /// </summary>
        private static TelegramDispatcher m_Dispatcher = null;
        public static TelegramDispatcher Instance
        {
            get
            {
                if(m_Dispatcher == null)
                {
                    m_Dispatcher = new TelegramDispatcher();
                }
                return m_Dispatcher;
            }
        }
        */

        private SortedList<DateTime, Telegram> m_TelegramQ = new SortedList<DateTime, Telegram>();
        public void DispatchTelegram(double delayInSeconds, Guid from, Guid to, MessageType type, Dictionary<string, object> parms, bool persist)
        {
            Telegram t = new Telegram();
            t.Sender = from;
            t.Recipient = to;
            t.MessageType = type;
            t.SendTime = DateTime.Now.AddSeconds(delayInSeconds);
            t.Parameters = new Shared.PropertyBag();

            IGameObject go = m_ActorManager.GetGameObjectFromId(t.Recipient);
            if(go == null || (!(go is IMessagable)))
            {
                System.Diagnostics.Trace.WriteLine(from.ToString() + " is attempting to send a message to non-registered entity " + to.ToString());
                return;
            }

            if(delayInSeconds == 0)
            {
                ((IMessagable)go).HandleTelegram(t);
                return;
            }

            m_TelegramQ.Add(t.SendTime, t);
        }



    }
}
