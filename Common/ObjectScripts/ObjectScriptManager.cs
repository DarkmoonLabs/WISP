using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared
{
    public class ObjectScriptManager : ISerializableWispObject
    {
        private IGameObject m_OwningObject;

        public IGameObject OwningObject
        {
            get { return m_OwningObject; }
        }
        

        #region Scripts


        public ObjectScriptManager(IGameObject owningObject)
        {
            m_OwningObject = owningObject;
            AttachedScripts = new Dictionary<uint, GameObjectScript>();
        }
        public Dictionary<uint, GameObjectScript> AttachedScripts { get; set; }
      
        public List<GameObjectScript> GetScripts(GameEventType filter)
        {
            List<GameObjectScript> listeners = null;
            if (!ScriptEventReceivers.TryGetValue(filter, out listeners))
            {
                return new List<GameObjectScript>();
            }

            return listeners;
        }

        public bool AttachScript(uint kind, bool fireScriptAttachEvent = false)
        {
            GameObjectScript e = GameObjectScript.GetScript(kind);
            if (e == null)
            {
                return false;
            }
              
            return AttachScript(e);
        }
        
        public bool AttachScript(GameObjectScript e , bool fireScriptAttachEvent = false)
        {
            lock (AttachedScripts)
            {
                if(AttachedScripts.Values.Contains(e))
                {
                    return false;
                }
                // Add it to the local effects list on the character object
                AttachedScripts.Add(e.TypeHash, e);

                // Fire the attached even so that the effect can set itself up and
                // so that it gets registered with the game event system
                if (fireScriptAttachEvent)
                {
                    e.OnAttach(m_OwningObject);
                }

                foreach(GameEventType eid in e.ListeningEvents)
                {
                    SubscribeToEvent(eid, e);
                }
            }
            return true;
        }

        public bool DetachScript(GameObjectScript e, bool fireDetachScriptEvent = false)
        {
            if (AttachedScripts.Remove(e.TypeHash))
            {
                if (fireDetachScriptEvent)
                {
                    e.OnDetach(m_OwningObject);
                }
                return true;
            }

            foreach (GameEventType eid in e.ListeningEvents)
            {
                UnSubscribeFromEvent(eid, e);
            }

            return false;
        }

        public virtual bool CanAttachScript(uint kind)
        {
            return true;
        }

        

        #endregion

        private static uint m_TypeHash = 0;
        public uint TypeHash
        {
            get
            {
                if (m_TypeHash == 0)
                {
                    m_TypeHash = Factory.GetTypeHash(this.GetType());
                }

                return m_TypeHash;
            }
        }

        void ISerializableWispObject.Serialize(ref byte[] buffer, Pointer p)
        {
            
        }

        void ISerializableWispObject.Deserialize(byte[] data, Pointer p)
        {
            
        }

        /// <summary>
        /// which scripts are listening to which events.
        /// </summary>
        private Dictionary<GameEventType, List<GameObjectScript>> ScriptEventReceivers = new Dictionary<GameEventType, List<GameObjectScript>>();

        /// <summary>
        /// Lets the script manager know that a GameObjectScript is interested in an event 
        /// </summary>
        /// <param name="listener">the object to receive the notifications</param>
        private void SubscribeToEvent(GameEventType eventId, GameObjectScript receiver)
        {
            lock(ScriptEventReceivers)
            {
                List<GameObjectScript> listeners = null;
                if(!ScriptEventReceivers.TryGetValue(eventId, out listeners))
                {
                    listeners = new List<GameObjectScript>();
                    listeners.Add(receiver);
                    ScriptEventReceivers.Add(eventId, listeners);
                }
                
                if(!listeners.Contains(receiver))
                {
                    listeners.Add(receiver);
                }
            }            
        }

        /// <summary>
        /// Stop listening to property change notifications on this bag
        /// </summary>
        /// <param name="listener">the object to no longer receive notifications</param>
        private void UnSubscribeFromEvent(GameEventType eventId, GameObjectScript receiver)
        {
            lock (ScriptEventReceivers)
            {
                List<GameObjectScript> listeners = null;
                if (ScriptEventReceivers.TryGetValue(eventId, out listeners))
                {
                    listeners.Remove(receiver);
                }
            }
        }
    }
}
