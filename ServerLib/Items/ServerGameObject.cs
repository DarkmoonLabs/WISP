using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameLib;

namespace Shared
{
    public class ServerGameObject : GenericGameObject, IStatBagOwner, IPropertyBagOwner
    {       
        public ServerGameObject() : base()
        {
            Properties.SubscribeToChangeNotifications(this);
            Stats.SubscribeToChangeNotifications(this);
            IsTransient = true;
            IsZombie = false;
            Version = 0;
        }

        public bool IsTransient { get; set; }
        public string OwningServer { get; set; }

        /// <summary>
        /// When static items are persisted, they only have their template stored in the DB.  All data (or scripts not listed in the template definition file) that 
        /// you set on a static item during runtime, will not be persisted.  When a static item is loaded from the DB, it will contain only data and scripts specified in the
        /// template file.  If you make a change to the template file, ALL items matching that template will be updated.
        /// </summary>
        private bool m_IsStatic;

        public bool IsStatic
        {
            get { return m_IsStatic; }
            set 
            {
                m_IsStatic = value; 
            }
        }
        

        /// <summary>
        /// A ghost object is one that only exists in server memory and not in the DB.
        /// When an object is first created, it is a ghost.  During the next save cycle,
        /// the object will be persisted to the DB and will no longer be marked as ghost.
        /// </summary>
        public bool IsGhost { get; set; }

        /// <summary>
        /// Zombie objects need to be removed from server tracking after the next save cycle.
        /// </summary>
        public bool IsZombie { get; set; }

        /// <summary>
        /// Deleted objects are still being tracked by the server until the next save cycle when they are marked as deleted
        /// in the DB and then no longer tracked in server memory
        /// </summary>
        public bool IsDeleted { get; set; }

        /// <summary>
        /// If an object IsDeleted, DeleteReason tells us why.  This is for service log purposes.
        /// </summary>
        public string DeleteReason { get; set; }

        /// <summary>
        /// if an object IsDeleted, AccountDeleted tells us who possessed it or who caused the deletion.
        /// </summary>
        public Guid AccountDeleted { get; set; }

        private bool m_IsDirty = true;
        /// <summary>
        /// Only dirty objects are saved to the DB during the save cycle.
        /// Modifying an object's script bag, statbag or propertybag will mark the object as dirty.
        /// </summary>
        public bool IsDirty 
        { 
            get
            {
                return m_IsDirty;
            }
            set
            {
                m_IsDirty = value;
                if (value && IsSaving)// save cycle code is currently saving the object and will attempt to set it IsDirty to false when it returns, but we need to save the changes that are causing this method to fire next round, so persist IsDirty even when the save cycle is done
                {
                    m_PersistDirty = true;
                }                
            }
        }
               
        /// <summary>
        /// When an item is in the processing of being written to the DB
        /// </summary>
        public bool IsSaving
        {
            get { return m_IsSaving; }
            set 
            {
                if (!value) // save is done
                {
                    if (m_PersistDirty)
                    {
                        m_PersistDirty = false;
                        IsDirty = true;
                        return;
                    }
                }

                m_IsSaving = value; 
            }
        }
        private bool m_IsSaving;
        
        /// <summary>
        /// If changes are made to the object while it is being saved, we must persist the dirty state so it gets saved again next save cycle
        /// </summary>
        private bool m_PersistDirty = false;

        public virtual void HandleTelegram(Telegram t)
        {
        }

        public void OnPropertyUpdated(Guid bag, Property p)
        {
            IsDirty = true;
        }

        public void OnPropertyAdded(Guid bag, Property p)
        {
            IsDirty = true;
        }    

        public void OnPropertyRemoved(Guid bag, Property p)
        {
            IsDirty = true;
        }
       
        public void OnStatAdded(Guid bag, Stat p)
        {
            IsDirty = true;        
        }

        public void OnStatRemoved(Guid bag, Stat p)
        {
            IsDirty = true;
        }

        public void OnStatUpdated(Guid bag, Stat s)
        {
            IsDirty = true;
        }

        public override void Serialize(ref byte[] buffer, Pointer p)
        {
            base.Serialize(ref buffer, p);
            BitPacker.AddInt(ref buffer, p, Version);
        }

        public override void Deserialize(byte[] data, Pointer p)
        {
            base.Deserialize(data, p);
            Version = BitPacker.GetInt(data, p);            
        }

        /// <summary>
        /// Serialization version
        /// </summary>
        public int Version { get; set; }

        
    }
}
