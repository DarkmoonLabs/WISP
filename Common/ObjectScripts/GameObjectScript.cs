using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared
{
    public enum EffectDurationType
    {
        Time = 1,
        Turns = 2
    }

    /// <summary>
    /// Can be attached to game objects to respond to game events and add general functionality
    /// </summary>
    public abstract class GameObjectScript
    {
        public GameObjectScript()
        {            
        }

        public void SubscribeToEvent(GameEventType eventId)
        {
            lock(EventsToListenTo)
            {
                if(EventsToListenTo.Contains(eventId))
                {
                    return;
                }
                EventsToListenTo.Add(eventId);
            }
        }
        
        public void UnsubscribeFromEvent(GameEventType eventId)
        {
            lock (EventsToListenTo)
            {
                EventsToListenTo.Remove(eventId);
            }
        }

        public abstract uint TypeHash
        {
            get;
        }

        /// <summary>
        /// The events that this script wants to be notified for
        /// </summary>
        protected List<GameEventType> EventsToListenTo = new List<GameEventType>();

        public GameEventType[] ListeningEvents
        {
            get
            {
                return EventsToListenTo.ToArray();
            }
        }

        public static Dictionary<uint, GameObjectScript> InstantiatedScripts = new Dictionary<uint, GameObjectScript>();

        public bool IsScriptListeningToEvent(GameEventType eventType)
        {
            return EventsToListenTo.Contains(eventType);
        }

        public static GameObjectScript GetScript(uint kind)
        {
            GameObjectScript e = null;
            if(!InstantiatedScripts.TryGetValue(kind, out e))
            {
                lock (InstantiatedScripts)
                {
                    e = Factory.Instance.CreateObject(kind) as GameObjectScript;
                    if (e == null)
                    {
                        return null;
                    }

                    InstantiatedScripts.Remove(kind);
                    InstantiatedScripts.Add(kind, e);
                }              
            }
            
            return e;
        }

        /// <summary>
        /// Script attached
        /// </summary>
        /// <returns></returns>
        public virtual void OnAttach(IGameObject thisGameObject)
        {
        }

        /// <summary>
        /// Script detached
        /// </summary>
        /// <returns></returns>
        public virtual void OnDetach(IGameObject thisGameObject)
        {
        }

        public virtual bool BeforeEventOccured(GameEventType eventKind, IGameObject thisGameObject, IGameObject instigator, Dictionary<string, object> args)
        {
            return true;
        }

        public virtual void EventOccured(GameEventType eventKind, IGameObject thisGameObject, IGameObject instigator, Dictionary<string, object> args)
        {
        }



    }
}
