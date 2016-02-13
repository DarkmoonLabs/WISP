using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared
{
    public class GenericGameObject : IGameObject
    {
        public GenericGameObject() : base()
        {
            Scripts = new ObjectScriptManager(this);
            Effects = new EffectManager(this);

            m_UID = Guid.NewGuid();
            CreatedOn = DateTime.Now;
            ItemTemplate = "";
            Stats = new StatBag();
            Properties = new PropertyBag();
            ObjectName = "";
        }

        public Guid Context { get; set; }

        private string m_ItemName;
        /// <summary>
        /// User friendly title of the POI
        /// </summary>
        public string ObjectName
        {
            get { return m_ItemName; }
            set { m_ItemName = value; }
        }

        public virtual void HandleTelegram(Telegram t)
        {

        }

        /// <summary>
        /// The game object which owns this game object.  Might be a container, or perhaps a player.
        /// </summary>
        public Guid Owner { get; set; }

        public DateTime CreatedOn
        {
            get;
            set;
        }

        public string ItemTemplate
        {
            get;
            set;
        }

        private GOT m_GameObjectType = GOT.GenericItem;
        public GOT GameObjectType
        {
            get
            {
                return m_GameObjectType;
            }
            set
            {
                m_GameObjectType = value;
            }
        }

        public PropertyBag Properties
        {
            get;
            set;
        }

        public StatBag Stats
        {
            get;
            set;
        }

        public ObjectScriptManager Scripts
        {
            get;
            set;
        }
        public EffectManager Effects
        {
            get;
            set;
        }

        private object m_Tag = null;
        public object Tag
        {
            get
            {
                return m_Tag;
            }
            set
            {
                m_Tag = value;
            }
        }


        private Guid m_UID = Guid.Empty;

        /// <summary>
        /// Unique ID
        /// </summary>
        public Guid UID
        {
            get
            {
                return m_UID;
            }
            set
            {
                m_UID = value;
            }

        }

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
            BitPacker.AddInt(ref buffer, p, Scripts.AttachedScripts.Count);
            foreach(GameObjectScript s in Scripts.AttachedScripts.Values)
            {
                BitPacker.AddUInt(ref buffer, p, s.TypeHash);
            }

            BitPacker.AddInt(ref buffer, p, Effects.AttachedEffects.Count);
            foreach (Effect e in Effects.AttachedEffects.Values)
            {
                BitPacker.AddUInt(ref buffer, p, e.Information.EffectKind);
                e.Serialize(ref buffer, p);
            }

            BitPacker.AddDouble(ref buffer, p, PhysicalState.Position.X);
            BitPacker.AddDouble(ref buffer, p, PhysicalState.Position.Y);
            BitPacker.AddDouble(ref buffer, p, PhysicalState.Position.Z);

            BitPacker.AddDouble(ref buffer, p, PhysicalState.Rotation.X);
            BitPacker.AddDouble(ref buffer, p, PhysicalState.Rotation.Y);
            BitPacker.AddDouble(ref buffer, p, PhysicalState.Rotation.Z);
        }

        public virtual void Deserialize(byte[] data, Pointer p)
        {
            int numScripts = BitPacker.GetInt(data, p);
            for (int i = 0; i < numScripts; i++ )
            {
                uint scriptId = BitPacker.GetUInt(data, p);
                Scripts.AttachScript(scriptId);
            }

            int numEffects = BitPacker.GetInt(data, p);
            for (int i = 0; i < numEffects; i++)
            {
                uint effectId = BitPacker.GetUInt(data, p);
                Effect e = Effect.GetEffect(effectId);

                if (e != null)
                {
                    e.Deserialize(data, p);
                }

                Effects.AttachEffect(this, null, effectId);
            }

            PhysicalState.Position.X = BitPacker.GetDouble(data, p);
            PhysicalState.Position.Y = BitPacker.GetDouble(data, p);
            PhysicalState.Position.Z = BitPacker.GetDouble(data, p);

            PhysicalState.Rotation.X = BitPacker.GetDouble(data, p);
            PhysicalState.Rotation.Y = BitPacker.GetDouble(data, p);
            PhysicalState.Rotation.Z = BitPacker.GetDouble(data, p);
        }

        public int StackCount { get; set; }

        /// <summary>
        /// Current position
        /// </summary>
        private MobileState m_PhysicalState = new MobileState();
        public MobileState PhysicalState
        {
            get
            {
                return m_PhysicalState;
            }
            set
            {
                m_PhysicalState = value;
            }
        }
    }
}
