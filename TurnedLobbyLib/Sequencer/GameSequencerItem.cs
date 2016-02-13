using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared
{
    public abstract class GameSequencerItem : ISerializableWispObject, IGameSequencerItem
    {
        public GameSequencerItem(int responseTimeOutMs)
        {
            ResponseTimeout = responseTimeOutMs;
        }

        /// <summary>
        /// The stack to which we belong
        /// </summary>
        public GameSequencer Sequencer { get; set; }

        private static uint m_TypeHash = 0;
        public virtual uint TypeHash
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

        public virtual void Serialize(ref byte[] buffer, Pointer p)
        {
        }

        public virtual void Deserialize(byte[] data, Pointer p)
        {
        }

        public virtual void OnBecameCurrent()
        {
        }

        public virtual void OnBecameNotCurrent()
        {
        }

        /// <summary>
        /// The amount of time that eligible players have to modify/augment/respond to this effect.
        /// If ResponseTimeout is > 0, the stack item's effect will not fire until that timeout is reached or until all eligible players have forfeited their response.
        /// This value is an initial value only. To interact with this value, use the Timer methods on the TurnStack object to which this TurnStackItem was added.
        /// </summary>
        public int ResponseTimeout { get; set; }

        /// <summary>
        /// The clock ticks at which time the ResponseTimeout out will have elapsed
        /// </summary>
        public long ResponseTime
        {
            get;
            set;
        }

        public bool HasBegunExecution { get; set; }
        public int ResponseTimerMod { get; set; }

        public virtual bool TryExecuteEffect(ref string msg  )
        {
            return true;
        }

    }
}
