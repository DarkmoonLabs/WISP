using System;
using System.Collections.Generic;
using System.Text;

namespace Shared
{
    /// <summary>
    /// Stores basic info about a character
    /// </summary>
    public class CharacterInfo : Component, ICharacterInfo, ISerializableWispObject
    {
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

        public CharacterInfo()
        {
            Properties = new PropertyBag();
            Stats = new StatBag();
        }

        public CharacterInfo(int id) : this()
        {
            ID = id;
        }

        public int ID { get; set; }

        public PropertyBag Properties { get; set; }

        public StatBag Stats { get; set; }

        public string CharacterName
        {
            get
            {
                return Properties.GetStringProperty((int)PropertyID.Name);
            }
            set
            {
                Properties.SetProperty((int)PropertyID.Name, value);
            }
        }

        public DateTime LastLogin
        {
            get
            {
                return Properties.GetDateTimeProperty((int)PropertyID.LastLogin).GetValueOrDefault(DateTime.MinValue);
            }
            set
            {
                Properties.SetProperty((int)PropertyID.LastLogin, value);
            }
        }

        /// <summary>
        /// If a character is loading into a level (i.e. physically loading the geometry, etc), their load percent will be less than 1.0f.  A fully loaded player is LoadedPercent 1.0f.
        /// </summary>
        public float LoadedPercent
        {
            get
            {
                return Properties.GetSinglelProperty("LoadedPercent").GetValueOrDefault(1);
            }
            set
            {
                Properties.SetProperty("LoadedPercent", value);
            }
        }

        public override void Serialize(ref byte[] buffer, Pointer p, bool includeComponents)
        {
            BitPacker.AddPropertyBag(ref buffer, p, Properties);
            BitPacker.AddStatBag(ref buffer, p, Stats);
            BitPacker.AddInt(ref buffer, p, ID);
         
            base.Serialize(ref buffer, p, includeComponents);
        }

        public override void Deserialize(byte[] data, Pointer p, bool includeComponents)
        {
            Properties = BitPacker.GetPropertyBag(data, p);
            Stats = BitPacker.GetStatBag(data, p);
            ID = BitPacker.GetInt(data, p);
            base.Deserialize(data, p, includeComponents);            
        }

     
    }
}
