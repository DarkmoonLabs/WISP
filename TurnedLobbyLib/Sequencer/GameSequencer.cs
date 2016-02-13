using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Runtime.CompilerServices;

namespace Shared
{
    /// <summary>
    /// Stores TurnSequence items in either a stack or a queue and activates those items based on timers associated with each item.
    /// This class is designed to allow the sequencing of game events and to optionally allow players to respond to each event within
    /// a given time frame.  
    /// </summary>
    public class GameSequencer : IDisposable
    {
        /// <summary>
        /// Initis a game sequencer either as a stack or a queue
        /// </summary>
        /// <param name="isStack">true to make this sequencer a queue, false to make it a stack</param>
        public GameSequencer(bool isQueue)
        {
            if (isQueue)
            {
                m_Queue = new Queue<GameSequencerItem>();
            }
            else
            {
                m_Stack = new Stack<GameSequencerItem>();
            }
        }

        /// <summary>
        /// Inits a sequencer implemented as a  queue
        /// </summary>
        public GameSequencer(ITurnedGame owningGame)
            : this(true)
        {
            OwningGame = owningGame;
        }

        public ITurnedGame OwningGame { get; set; }

        #region CurrentItemChanged Event
        private Action<IGameSequencerItem, IGameSequencerItem> CurrentItemChangedInvoker;

        /// <summary>
        /// Fires when a new item is either pushed from or popped onto the stack.  Includes old (arg1) and new (arg2) items
        /// </summary>
        public event Action<IGameSequencerItem, IGameSequencerItem> CurrentItemChanged
        {
            add
            {
                AddHandler_CurrentItemChanged(value);
            }
            remove
            {
                RemoveHandler_CurrentItemChanged(value);
            }
        }

        
        private void AddHandler_CurrentItemChanged(Action<IGameSequencerItem, IGameSequencerItem> value)
        {
            CurrentItemChangedInvoker = (Action<IGameSequencerItem, IGameSequencerItem>)Delegate.Combine(CurrentItemChangedInvoker, value);
        }

        
        private void RemoveHandler_CurrentItemChanged(Action<IGameSequencerItem, IGameSequencerItem> value)
        {
            CurrentItemChangedInvoker = (Action<IGameSequencerItem, IGameSequencerItem>)Delegate.Remove(CurrentItemChangedInvoker, value);
        }

        private void FireCurrentItemChanged(IGameSequencerItem oldItem, IGameSequencerItem newItem)
        {
            if (CurrentItemChangedInvoker != null)
            {
                CurrentItemChangedInvoker(oldItem, newItem);
            }
        }
        #endregion
        
        #region ItemExecuted Event
        private Action<IGameSequencerItem, bool, string> ItemExecutedInvoker;

        /// <summary>
        /// Fires when an item's effect is executed.  Includes the item, the result, and a message (if any)
        /// </summary>
        public event Action<IGameSequencerItem, bool, string> ItemExecuted
        {
            add
            {
                AddHandler_ItemExecuted(value);
            }
            remove
            {
                RemoveHandler_ItemExecuted(value);
            }
        }

        
        private void AddHandler_ItemExecuted(Action<IGameSequencerItem, bool, string> value)
        {
            ItemExecutedInvoker = (Action<IGameSequencerItem, bool, string>)Delegate.Combine(ItemExecutedInvoker, value);
        }

        
        private void RemoveHandler_ItemExecuted(Action<IGameSequencerItem, bool, string> value)
        {
            ItemExecutedInvoker = (Action<IGameSequencerItem, bool, string>)Delegate.Remove(ItemExecutedInvoker, value);
        }

        private void FireItemExecuted(IGameSequencerItem item, bool result, string msg)
        {
            if (ItemExecutedInvoker != null)
            {
                ItemExecutedInvoker(item, result, msg);
            }
        }
        #endregion

        #region ItemResponseTimerStarted Event
        private Action<IGameSequencerItem> ItemResponseTimerStartedInvoker;

        /// <summary>
        /// Fires when a TurnStackItem with a ResponseTimers > 0 is made the current item in the stack. When the timer elapses, 
        /// the item will be executed
        /// </summary>
        public event Action<IGameSequencerItem> ItemResponseTimerStarted
        {
            add
            {
                AddHandler_ItemResponseTimerStarted(value);
            }
            remove
            {
                RemoveHandler_ItemResponseTimerStarted(value);
            }
        }

        
        private void AddHandler_ItemResponseTimerStarted(Action<IGameSequencerItem> value)
        {
            ItemResponseTimerStartedInvoker = (Action<IGameSequencerItem>)Delegate.Combine(ItemResponseTimerStartedInvoker, value);
        }

        
        private void RemoveHandler_ItemResponseTimerStarted(Action<IGameSequencerItem> value)
        {
            ItemResponseTimerStartedInvoker = (Action<IGameSequencerItem>)Delegate.Remove(ItemResponseTimerStartedInvoker, value);
        }

        private void FireItemResponseTimerStarted(IGameSequencerItem item)
        {
            if (ItemResponseTimerStartedInvoker != null)
            {
                ItemResponseTimerStartedInvoker(item);
            }
        }
        #endregion

        private Timer m_Timer;

        /// <summary>
        /// The number of items currently in the stack
        /// </summary>
        public int ItemCount
        {
            get
            {
                lock (CurrentItemSyncRoot)
                {
                    if (m_Queue != null)
                    {
                        return m_Queue.Count;
                    }
                    return m_Stack.Count;
                }
            }
        }

        /// <summary>
        /// The current item on the turn stack
        /// </summary>
        public IGameSequencerItem CurrentItem
        {
            get
            {
                lock (CurrentItemSyncRoot)
                {
                    return m_CurrentItem;
                }
            }
            private set
            {
                lock (CurrentItemSyncRoot)
                {
                    IGameSequencerItem old = m_CurrentItem;
                    m_CurrentItem = value;
                    if (old != value)
                    {
                        CancelTimerForCurrentItem();
                        OnCurrentItemChanged(old, value);
                    }
                }
            }
        }
        private IGameSequencerItem m_CurrentItem;

        protected object CurrentItemSyncRoot = new object();

        private void SetTimerForCurrentItem(int ms)
        {
            if (ms < 1)
            {
                CancelTimerForCurrentItem();
                return;
            }

            if (m_Timer == null)
            {
                m_Timer = new Timer(new TimerCallback(OnTimerElapsed), null, ms, Timeout.Infinite);
            }
            else
            {
                m_Timer.Change(ms, Timeout.Infinite);
            }
        }

        private void CancelTimerForCurrentItem()
        {
            lock (CurrentItemSyncRoot)
            {
                if (m_Timer == null)
                {
                    return;
                }
                m_Timer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
            }
        }

        protected void OnTimerElapsed(object state)
        {
            lock (CurrentItemSyncRoot)
            {
                OnItemExecute(CurrentItem);
            }
        }

        private Stack<GameSequencerItem> m_Stack;
        private Queue<GameSequencerItem> m_Queue;

        /// <summary>
        /// Pushes a new item to the stack.  
        /// </summary>
        /// <param name="itm">the item to push</param>
        /// <param name="responseTimerMod">You can add/remove time from the response timer by passing a possitive or negative value as responseTimerMod.  Pass
        /// int.MinValue to disable the response timer and to cause the item to execute as soon as its on the stack. Pass int.MaxValue to never time the 
        /// stack item out.</param>
        public void AddItem(IGameSequencerItem itm, int responseTimerMod)
        {
            lock (CurrentItemSyncRoot)
            {
                itm.Sequencer = this;
                GameSequencerItem ex = itm as GameSequencerItem;
                ex.ResponseTimerMod = responseTimerMod;
                if (m_Queue != null)
                {
                    m_Queue.Enqueue(ex);
                }
                else
                {
                    m_Stack.Push(ex);
                }
            }
        }

        public void ClearSequence()
        {
            lock (CurrentItemSyncRoot)
            {
                CurrentItem = null;
                if (m_Queue != null)
                {
                    m_Queue.Clear();
                }
                else
                {
                    m_Stack.Clear();
                }
            }
        }

        public IGameSequencerItem ActivateNextItem()
        {
            lock (CurrentItemSyncRoot)
            {
                IGameSequencerItem itm = null;
                if (m_Queue != null)
                {
                    if (m_Queue.Count > 0)
                    {
                        itm = m_Queue.Dequeue();
                    }
                }
                else
                {
                    if (m_Stack.Count > 0)
                    {
                        itm = m_Stack.Pop();
                    }
                }

                if (itm != null)
                {
                    CurrentItem = itm;
                }

                return itm;
            }
        }

        protected virtual void OnCurrentItemChanged(IGameSequencerItem oldItem, IGameSequencerItem newItem)
        {
            if (oldItem != null)
            {
                oldItem.OnBecameNotCurrent();
            }

            if (newItem != null)
            {
                newItem.OnBecameCurrent();
            }

            FireCurrentItemChanged(oldItem, newItem);

            if (newItem == null)
            {
                return;
            }

            GameSequencerItem ex = newItem as GameSequencerItem;

            // Get the timeout
            int timeout =  0;
            if (ex.ResponseTimerMod == int.MaxValue)
            {
                timeout = Timeout.Infinite;
            }
            else if (ex.ResponseTimerMod > int.MinValue)
            {
                timeout = ex.ResponseTimeout + ex.ResponseTimerMod;
            }

            ex.ResponseTimeout = timeout;            
            
            // Set the timer or execute in case of no timeout
            if (timeout > 0)
            {
                Log.LogMsg(" -> Waiting [" + TimeSpan.FromMilliseconds(timeout).TotalSeconds + "] seconds before executing.");
                ex.ResponseTime = DateTime.UtcNow.Ticks + TimeSpan.FromMilliseconds(timeout).Ticks;
                SetTimerForCurrentItem(timeout);
                OnItemResponseTimerStarted(ex);

                DateTime exeTime = new DateTime(ex.ResponseTime, DateTimeKind.Utc);
                TimeSpan len = exeTime - DateTime.UtcNow;
                Log.LogMsg("It is now [" + DateTime.UtcNow.ToLongTimeString() + "]. Execute time for [" + ((Phase)ex).PhaseName + "] is at [" + exeTime.ToLongTimeString() + "], i.e. in [" + len.TotalSeconds + " seconds]. Response timeout is [" + timeout + " ms].");
            }
            else
            {
                OnItemExecute(ex);
            }
        }

        protected virtual void OnItemResponseTimerStarted(GameSequencerItem newItem)
        {            
            FireItemResponseTimerStarted(newItem);
        }

        protected virtual void OnItemExecute(IGameSequencerItem item)
        {
            if (CurrentItem != item)
            {
                // it's possible that another thread Pushed or Popped the CurrentItem while the timer went off on the last current item
                return;
            }

            GameSequencerItem ex = item as GameSequencerItem;
            if (ex != null)
            {
                if (ex.HasBegunExecution) // dont execute twice, just in case
                {
                    return;
                }
                ex.HasBegunExecution = true;
            }

            string msg = "";
            bool rslt = true;
            rslt = item.TryExecuteEffect(ref msg);
            
            FireItemExecuted(ex, rslt, msg);
        }

        #region Dispose
        bool m_Disposed = false;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!m_Disposed)
            {
                if (disposing)
                {
                    if (m_Timer != null)
                    {
                        m_Timer.Dispose();
                        m_Timer = null;
                    }
                }

                // There are no unmanaged resources to release, but
                // if we add them, they need to be released here.
            }
            m_Disposed = true;

            // If it is available, make the call to the
            // base class's Dispose(Boolean) method
            //base.Disposed(disposing);
        }
        #endregion

    }
}
