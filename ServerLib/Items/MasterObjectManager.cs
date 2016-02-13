using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    /// <summary>
    /// Oversees game objects for ALL games on this server machine.  Objects are segmented by ContextId, i.e. game space ID
    /// </summary>
    public class MasterObjectManager
    {
        static MasterObjectManager()
        {
            ManagersByContext = new ConcurrentDictionary<Guid,ServerGameObjectManager>();
            AllGameObjects = new ConcurrentDictionary<Guid, ServerGameObject>();
        }

        private static ConcurrentDictionary<Guid, ServerGameObjectManager> ManagersByContext { get; set; }
        private static ConcurrentDictionary<Guid, ServerGameObject> AllGameObjects { get; set; }

        public static void Register(ServerGameObjectManager som)
        {
            ManagersByContext.TryAdd(som.Context, som);
            som.RunSaveCycle(ConfigHelper.GetStringConfig("ServerUserID", "EnterServerID"));
        }

        public static void UnRegister(ServerGameObjectManager som)
        {
            ServerGameObjectManager somt = null;
            somt.StopSaveCycle();
            ManagersByContext.TryRemove(som.Context, out somt);
        }

        public static bool RegisterObject(ServerGameObject go)
        {
            if(AllGameObjects.TryAdd(go.UID, go))
            {
                GameEvents.FireEvent(GameEventType.ObjectLoaded, go, null);
                return true;
            }
            return false;
        }
        
        public static bool UnregisterObject(Guid go)
        {
            ServerGameObject sgo = null;
            AllGameObjects.TryRemove(go, out sgo);
            return sgo != null;
        }

    }
}
