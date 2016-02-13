using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Shared;

namespace Shared
{
    public class GameEvents
    {

        public static void FireEvent(GameEventType eventKind, IGameObject targetObject, IGameObject instigator, Dictionary<string, object> args = null)
        {
            if (targetObject != null)
            {
                List<GameObjectScript> listeners = targetObject.Scripts.GetScripts(eventKind);
                foreach (GameObjectScript script in listeners)
                {
                    script.EventOccured(eventKind, targetObject, instigator, args);
                }
            }        
        }


    }
}
