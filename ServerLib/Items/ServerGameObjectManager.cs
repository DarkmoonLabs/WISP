using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Shared
{
    public class ServerGameObjectManager : GameObjectManager
    {
        public event Action<WispPlayer> OnPlayerAdded;
        public event Action<WispPlayer> OnPlayerRemoved;

        public ServerGameObjectManager(Guid context) : base(context)
        {
            MasterObjectManager.Register(this);
        }

        public override void RegisterGameObject(IGameObject actor, Guid context)
        {
            if (actor == null)
                return;

            base.RegisterGameObject(actor, context);
            ServerGameObject sgo = actor as ServerGameObject;
            MasterObjectManager.RegisterObject(sgo);

            if(actor is WispPlayer && OnPlayerAdded != null)
            {
                OnPlayerAdded(actor as WispPlayer);
            }
        }

        public override void RemoveGameObject(IGameObject actor)
        {
            if (actor == null)
                return;

            ServerGameObject sgo = actor as ServerGameObject;

            base.RemoveGameObject(actor);
            MasterObjectManager.UnregisterObject(actor.UID);

            if (actor is WispPlayer && OnPlayerRemoved != null)
            {
                OnPlayerRemoved(actor as WispPlayer);
            }
        }

        private bool m_StopSaving = false;

        /// <summary>
        /// Saves all currently loaded objects that are dirty to the database
        /// </summary>
        public void RunSaveCycle(string serverID)
        {

            if (m_StopSaving || 1 == Interlocked.Exchange(ref m_UpdatingSaveQueue, 1))
            {
                // Failed to get the lock.  Process is already/still running.
                return;
            }
            Interlocked.Exchange(ref m_SaveCycleScheduled, 0);

            DateTime start = DateTime.UtcNow;
            ICollection<IGameObject> items = AllObjects;
            string msg = "";
            int processed = DB.Instance.Item_BatchUpdate((List<IGameObject>)items, out msg, this);

            DateTime fin = DateTime.UtcNow;
            TimeSpan len = fin - start;
            Log1.Logger("Server").Info("Game object save cycle wrote  [" + processed.ToString() + " objects in " + len.ToString() + "]");
            Log1.Logger("Server").Info("Server is now tracking [" + ObjectCount.ToString() + " game objects].");

            StartSaveCycle(serverID, len.TotalMilliseconds > ConfigHelper.GetIntConfig("ItemSaveCycleTimerMs", 30000));

            // Release the lock
            Interlocked.Exchange(ref m_UpdatingSaveQueue, 0);
        }

        private long m_SaveCycleScheduled = 0;
        /// <summary>
        /// Used to establish a lock on the save queue process
        /// </summary>
        private long m_UpdatingSaveQueue = 0;

        public async Task StartSaveCycle(string serverID, bool noDelay = false)
        {
            if (1 == Interlocked.Exchange(ref m_SaveCycleScheduled, 1))
            {
                // Failed to get the lock.  Cycle already/still scheduled.
                return;
            }

            await Task.Delay(noDelay ? 5 : ConfigHelper.GetIntConfig("ItemSaveCycleTimerMs", 30000));
            RunSaveCycle(serverID);
        }

        public void StopSaveCycle()
        {
            m_StopSaving = true;
        }

        
    }
}
