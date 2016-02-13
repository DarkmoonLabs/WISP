using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared
{
    /// <summary>
    ///  A character, as represented on a game server
    /// </summary>
    public class ServerCharacterInfo : ServerCharacterComponent, ICharacterInfo, IStatBagOwner, IPropertyBagOwner
    {        
        /// <summary>
        /// The character info object that this ServerCharacterInfo object is decorating
        /// </summary>
        public CharacterInfo CharacterInfo
        {
            get { return m_CharacterInfo; }
            set { m_CharacterInfo = value; }
        }
        private CharacterInfo m_CharacterInfo;

        public override PropertyBag AddedProperties
        {
            get
            {
                return this.Properties;
            }
        }

        public override StatBag AddedStats
        {
            get
            {
                return this.Stats;
            }
        }

        public override void AddComponent(IComponent c)
        {
            base.AddComponent(c);
            ServerCharacterComponent com = c as ServerCharacterComponent;
            if (com != null)
            {                
                Stats.UpdateWithValues(com.AddedStats);

                if (com.AddedProperties != null)
                {
                    // initial copy of properties
                    Properties.UpdateWithValues(com.AddedProperties);
                    
                    // be notified of future updates. Two-way binding
                    com.AddedProperties.SubscribeToChangeNotifications(this);
                    Properties.SubscribeToChangeNotifications(com);
                }

                if (com.AddedStats!= null)
                {
                    // initial copy of properties
                    Stats.UpdateWithValues(com.AddedStats);
                    
                    // be notified of future updates. Two-way binding.
                    com.AddedStats.SubscribeToChangeNotifications(this);
                    Stats.SubscribeToChangeNotifications(com);
                }
            }
        }

        public override void RemoveComponent(IComponent c)
        {
            base.RemoveComponent(c);
            ServerCharacterComponent com = c as ServerCharacterComponent;
            if (com != null)
            {
                com.AddedStats.UnSubscribeToChangeNotifications(this);
                com.AddedProperties.UnSubscribeToChangeNotifications(this);

                if (Components.Count < 1)
                {
                    Properties.UnSubscribeToChangeNotifications(com);
                    Stats.UnSubscribeToChangeNotifications(com);
                }

                Stats.RemoveStats(com.AddedStats);
                Properties.RemoveProperties(com.AddedProperties);
            }
        }

        /// <summary>
        /// Even if "Owner" == null, the OwnerId should still be set.
        /// </summary>
        public Guid Owner
        {
            get
            {
                if (OwningAccount == null)
                {
                    return m_Owner;
                }

                return OwningAccount.ID;
            }
            set
            {
                m_Owner = value;
            }
        }
        private Guid m_Owner = Guid.Empty;

        /// <summary>
        /// The user that this character belongs to
        /// </summary>
        public ServerUser OwningAccount
        {
            get { return m_OwningAccount; }
            set { m_OwningAccount = value; }
        }
        private ServerUser m_OwningAccount;

        public Guid TargetResource { get; set; }        
       
        private static uint m_TypeHash = 0;
        public override uint TypeHash
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

        protected virtual void OnInitialize()
        {
        }

        public ServerCharacterInfo(CharacterInfo characterInfo) : this(characterInfo, null)
        {

        }

        public ServerCharacterInfo(CharacterInfo characterInfo, ServerUser owner)
        {            
            OwningAccount = owner;
            m_CharacterInfo = characterInfo;
            OnInitialize();
        }

        public ServerCharacterInfo(CharacterInfo characterInfo, Guid ownerId) : this(characterInfo, null)
        {
            m_Owner = ownerId;
        }

        public ServerCharacterInfo() : this(new CharacterInfo(), null)
        {
        }

        public override void Serialize(ref byte[] buffer, Pointer p, bool includeComponents)
        {
            m_CharacterInfo.Serialize(ref buffer, p, includeComponents);

            BitPacker.AddString(ref buffer, p, OwningAccount.OwningServer);
            BitPacker.AddString(ref buffer, p, OwningAccount.ID.ToString());
            BitPacker.AddString(ref buffer, p, OwningAccount.AccountName);
            BitPacker.AddSerializableWispObject(ref buffer, p, OwningAccount.Profile);

            BitPacker.AddString(ref buffer, p, TargetResource.ToString());

            base.Serialize(ref buffer, p, includeComponents);
        }

        public override void Deserialize(byte[] data, Pointer p, bool includeComponents)
        {
            m_CharacterInfo = new Shared.CharacterInfo();
            m_CharacterInfo.Deserialize(data, p, includeComponents);

            ServerUser su = new ServerUser();
            su.AuthTicket = Guid.Empty;

            su.OwningServer = BitPacker.GetString(data, p);
            su.ID = new Guid(BitPacker.GetString(data, p));
            su.AccountName = BitPacker.GetString(data, p);

            su.Profile = (AccountProfile)BitPacker.GetSerializableWispObject(data, p);

            su.CurrentCharacter = this;
            this.TargetResource = new Guid(BitPacker.GetString(data, p));

            OwningAccount = su;

            base.Deserialize(data, p, includeComponents);
        }

        public string CharacterName
        {
            get
            {
                return m_CharacterInfo.CharacterName;
            }
            set
            {
                m_CharacterInfo.CharacterName = value;
            }
        }

        public int ID
        {
            get
            {
                return m_CharacterInfo.ID;
            }
            set
            {
                m_CharacterInfo.ID = value;
            }
        }

        public DateTime LastLogin
        {
            get
            {
                return m_CharacterInfo.LastLogin;
            }
            set
            {
                m_CharacterInfo.LastLogin = value;
            }
        }

        public PropertyBag Properties
        {
            get
            {
                return m_CharacterInfo.Properties;
            }
            set
            {
                m_CharacterInfo.Properties = value;
            }
        }

        public StatBag Stats
        {
            get
            {
                return m_CharacterInfo.Stats;
            }
            set
            {
                m_CharacterInfo.Stats = value;
            }
        }

        public void OnPropertyUpdated(Guid bag, Property p)
        {
            if (p.Owner != Properties)
            {
                // then it's bubbled up from outside of this object. update our properties with it
                Properties.AddProperty(p);
            }
        }

        public void OnStatUpdated(Guid bag, Stat s)
        {
            if (s.Owner != Stats)
            {
                // then it's bubbled up from outside of this object. update our stats with it
                Stats.AddStat(s);
            }
        }  
    }
}
