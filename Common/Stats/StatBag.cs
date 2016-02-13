using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared
{
    /// <summary>
    /// A collection of stats
    /// </summary>
    public class StatBag
    {
        /// <summary>
        /// The stat bag will use a linked list for storing the stats until it becomes more efficient to use a dictionary/hashtable.
        /// This value indicates what the maximum number of stats is before we switch over to dictionary storage
        /// </summary>
        public static int SmallSizeLimit = 25;

        // Stat storage ////////////
        private System.Collections.Generic.Dictionary<int, Stat> m_HashedProperties;
        private LinkedList<Stat> m_LinkedProperties;
        private bool m_UsingLinkedStorage = true;
        private List<IStatBagOwner> m_Listeners = new List<IStatBagOwner>();
        ///////////////////////////////



        /// <summary>
        /// A name for this stat bag.  
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// A GUID for this stat bag.  Use this to keep multiple bags on the game object apart from one another
        /// </summary>
        public Guid ID { get; set; }


        /// <summary>
        /// The number of properties in the bag
        /// </summary>
        public int StatCount
        {
            get
            {
                if (m_UsingLinkedStorage)
                {
                    return m_LinkedProperties.Count;
                }
                return m_HashedProperties.Count;
            }
        }

        /// <summary>
        /// All the properties contained in this bag
        /// </summary>
        public Stat[] AllStats
        {
            get
            {
                if (m_UsingLinkedStorage)
                {
                    return m_LinkedProperties.ToArray();
                }

                return m_HashedProperties.Values.ToArray();
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public StatBag(string description)
        {
            Name = description;
            ID = Guid.NewGuid();

            if (SmallSizeLimit > 0)
            {
                m_LinkedProperties = new LinkedList<Stat>();
                m_UsingLinkedStorage = true;
            }
            else
            {
                m_UsingLinkedStorage = false;
                m_HashedProperties = new Dictionary<int, Stat>();
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public StatBag()
            : this("")
        {
        }

        /// <summary>
        /// retrieves a stat from the StatBag based on 
        /// the stat id
        /// </summary>
        public Stat GetStat(int statId)
        {
            // An instance of the stat that will be returned
            Stat objStat = null;

            // If the StatBag already contains a stat whose name matches
            // the stat required, ...
            if (m_UsingLinkedStorage)
            {
                objStat = m_LinkedProperties.FirstOrDefault(s => s.StatID == statId);
                if(objStat != null)
                {
                        // Optimization: move element to beginning of list for optimization.  basically the most requested properties will be
                        // at the beginning of the list, eventually
                        m_LinkedProperties.Remove(objStat);
                        m_LinkedProperties.AddFirst(objStat);
                }
            }
            else
            {
                m_HashedProperties.TryGetValue(statId, out objStat);
            }

            return objStat;
        }

        public Stat GetStat(string name)
        {
            // An instance of the stat that will be returned
            Stat objStat = null;

            if (m_UsingLinkedStorage)
            {
                objStat = m_LinkedProperties.FirstOrDefault(s => s.DisplayName.ToLower() == name.ToLower());
            }
            else
            {
                objStat = m_HashedProperties.Values.FirstOrDefault(s => s.DisplayName.ToLower() == name.ToLower());
            }

            return objStat;
        }

        /// <summary>
        /// Removes a stat object from the stat bag that has the same ID as @objStat
        /// </summary>
        /// <param name="objStat">the stat to add</param>
        public void RemoveStat(Stat objStat)
        {
            bool wasRemoved = false;
            if (m_HashedProperties != null)
            {
                wasRemoved = m_HashedProperties.Remove(objStat.StatID);
            }
            else
            {
                Stat cur = m_LinkedProperties.FirstOrDefault(s => s.StatID == objStat.StatID);
                if (cur != null)
                {
                    wasRemoved = m_LinkedProperties.Remove(cur);
                }
            }
            
            objStat.Owner = null;
            if (wasRemoved)
            {
                NotifyStatRemoved(objStat);
            }
        }
        
        /// <summary>
        /// Adds a stat object to the stat bag
        /// </summary>
        /// <param name="objStat">the stat to add</param>
        public void AddStat(Stat objStat)
        {
            Stat current = GetStat(objStat.StatID);
            bool wasAdded = current == null;

            if (current != null)
            {
                if (m_HashedProperties != null)
                {
                    m_HashedProperties.Remove(objStat.StatID);
                }
                else
                {
                    Stat cur = m_LinkedProperties.FirstOrDefault(s => s.StatID == objStat.StatID);
                    if (cur != null)
                    {
                        m_LinkedProperties.Remove(cur);
                    }
                }
            }

            int propCount = StatCount;
            if (propCount + 1 > StatBag.SmallSizeLimit && m_UsingLinkedStorage)
            {
                m_UsingLinkedStorage = false;

                // time to switch to hashtable lookups.  move the data over
                m_HashedProperties = new Dictionary<int, Stat>();
                LinkedList<Stat>.Enumerator enu = m_LinkedProperties.GetEnumerator();
                while (enu.MoveNext())
                {
                    m_HashedProperties.Add(enu.Current.StatID, enu.Current);
                }

                m_LinkedProperties.Clear();
                m_LinkedProperties = null;
            }

            if (m_UsingLinkedStorage)
            {
                m_LinkedProperties.AddLast(objStat);
            }
            else
            {
                m_HashedProperties.Add(objStat.StatID, objStat);
            }

            objStat.Owner = this;

            if (wasAdded)
            {
                NotifyStatAdded(objStat);
            }
            else
            {
                NotifyStatUpdated(objStat);
            }
        }

        /// <summary>
        /// Start listening to stat change notifications on this bag
        /// </summary>
        /// <param name="listener">the object to receive the notifications</param>
        public void SubscribeToChangeNotifications(IStatBagOwner listener)
        {
            m_Listeners.Remove(listener);
            m_Listeners.Add(listener);
        }

        /// <summary>
        /// Stop listening to stat change notifications on this bag
        /// </summary>
        /// <param name="listener">the object to no longer receive notifications</param>
        public void UnSubscribeToChangeNotifications(IStatBagOwner listener)
        {
            m_Listeners.Remove(listener);
        }

        /// <summary>
        /// Sends out notifications that a stat has been updated
        /// </summary>
        /// <param name="p">the stat that was updated</param>
        public void NotifyStatUpdated(Stat p)
        {
            for (int i = 0; i < m_Listeners.Count; i++)
            {
                m_Listeners[i].OnStatUpdated(this.ID, p);
            }
        }

        /// <summary>
        /// Checks to see if the given stat is currently in the bag
        /// </summary>
        /// <param name="id"></param>
        public bool HasStat(int id)
        {
            if (m_UsingLinkedStorage)
            {
                Stat cur = m_LinkedProperties.FirstOrDefault(s => s.StatID == id);
                if (cur != null)
                {
                    return true;
                }
                return false;
            }
            else
            {
                return m_HashedProperties.ContainsKey(id);
            }
        }

        /// <summary>
        /// Sends out notifications that a Stat has been added
        /// </summary>
        /// <param name="p">the Stat that was updated</param>
        public void NotifyStatAdded(Stat p)
        {
            for (int i = 0; i < m_Listeners.Count; i++)
            {
                m_Listeners[i].OnStatAdded(this.ID, p);
            }
        }

        /// <summary>
        /// Sends out notifications that a Stat has been removed
        /// </summary>
        /// <param name="p">the Stat that was updated</param>
        public void NotifyStatRemoved(Stat p)
        {
            for (int i = 0; i < m_Listeners.Count; i++)
            {
                m_Listeners[i].OnStatRemoved(this.ID, p);
            }
        }

        public float? GetStatValue(int statId)
        {
            Stat s = GetStat(statId);
            if (s == null)
            {
                return null;
            }

            return s.CurrentValue;
        }

        public float GetStatValue(int statId, float orDefault)
        {
            Stat s = GetStat(statId);
            if (s == null)
            {
                return orDefault;
            }

            return s.CurrentValue;
        }

        public void SetStatValue(int statId, float val, List<string> msgs)
        {
            Stat stat = GetStat(statId);

            if (stat == null)
            {
                Stat newSTat = new Stat(statId, "", "", "", val, float.MinValue, float.MaxValue);
                newSTat.Owner = this;                
                AddStat(newSTat);
                return;          
            }

            if (stat.CurrentValue != val)
            {
                stat.SetValue(val, msgs);
                NotifyStatUpdated(stat);
            }
        }

        public void SetStatValue(int statId, float val, float maxVal, List<string> msgs)
        {
            Stat stat = GetStat(statId);
            if (stat == null)
            {
                Stat newSTat = new Stat(statId, "", "", "", val, float.MinValue, float.MaxValue);
                newSTat.Owner = this;
                AddStat(newSTat);
                return;
            }

            if (stat.CurrentValue != val || stat.MaxValue != maxVal)
            {
                stat.SetValue(val, msgs);
                stat.MaxValue = maxVal;
                NotifyStatUpdated(stat);
            }
        }

        public void SetMaxStatValue(int statId, float val)
        {
            Stat stat = GetStat(statId);
            if (stat == null)
            {
                Stat newSTat = new Stat(statId, "", "", "", -1, float.MinValue, val);
                newSTat.Owner = this;
                AddStat(newSTat);
                return;
            }

            if (stat.MaxValue != val)
            {
                stat.MaxValue = val;
                NotifyStatUpdated(stat);
            }
        }

        public void UpdateWithValues(StatBag statsToUpdate)
        {
            if (statsToUpdate == null)
            {
                return;
            }
            Stat[] stats = statsToUpdate.AllStats;
            for (int i = 0; i < stats.Length; i++)
            {
                Stat cloneStat = new Stat(stats[i].StatID, stats[i].DisplayName, stats[i].Description, stats[i].Group, stats[i].CurrentValue, stats[i].MinValue, stats[i].MaxValue);
                cloneStat.Owner = this;
                AddStat(cloneStat);
            }
        }

        public void RemoveStats(StatBag statsToRemove)
        {
            RemoveStats(statsToRemove.AllStats);
        }

        public void RemoveStats(Stat[] statsToRemove)
        {
            if (statsToRemove == null)
            {
                return;
            }

            foreach (Stat s in statsToRemove)
            {
                RemoveStat(s);
            }
        }


    }
}
