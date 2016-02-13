using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Shared;

namespace Shared
{
    public abstract class Component : PriorityQueueNode, ISerializableWispObject, IComponent
    {
        private string m_Name = "Anonymous Component";
        public string ComponentName
        {
            get { return m_Name; }
            set { m_Name = value; }
        }

        protected List<IComponent> m_Components = new List<IComponent>();

        public List<IComponent> Components
        {
            get
            {
                return m_Components;
            }
        }

        public virtual void AddComponent(IComponent c)
        {
            m_Components.Add(c);
        }

        public virtual void RemoveComponent(IComponent c)
        {
            m_Components.Remove(c);
        }

        public virtual List<T> GetComponentsOfType<T>() where T : IComponent
        {
            List<T> result = new List<T>();
#if !SILVERLIGHT
            result = m_Components.FindAll(cp => cp is T) as List<T>;
#else
            for (int i = 0; i < m_Components.Count; i++)
            {
                if(m_Components[i] is T)
                {
                    result.Add((T)m_Components[i]);
                }
            }
#endif
            return result;
        }

        public virtual T GetComponent<T>() where T : IComponent
        {
            T result = (T) m_Components.FirstOrDefault(cp => cp is T);
            return result;
        }

        public virtual void ClearComponents()
        {
            List<IComponent>.Enumerator enu = m_Components.GetEnumerator();
            while (enu.MoveNext())
            {
                enu.Current.ClearComponents();
            }

            m_Components.Clear();
        }

        public virtual IEnumerator<IComponent> GetComponentEnumerator()
        {
            return m_Components.GetEnumerator();
        }

        public virtual List<IComponent> GetAllComponents()
        {
            return m_Components;
        }

        /// <summary>
        /// Serializes the component to binary
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="p"></param>
        /// <param name="includeSubComponents"></param>
        public virtual void Serialize(ref byte[] buffer, Pointer p, bool includeSubComponents)
        {
            //Log.LogMsg("Serializing component " + GetType().ToString() + " as ID " + TypeHash.ToString());
            if (!includeSubComponents)
            {
                return;
            }
            lock (m_Components)
            {
                //List<Component> wisps = m_Components.FindAll(w => w is ISerializableWispObject);
                BitPacker.AddInt(ref buffer, p, m_Components.Count);
                for (int i = 0; i < m_Components.Count; i++)
                {
                    IComponent wisp = m_Components[i] as IComponent;
                    BitPacker.AddUInt(ref buffer, p, wisp.TypeHash);                    
                    wisp.Serialize(ref buffer, p, includeSubComponents);
                }
            }
        }

        public virtual void Deserialize(byte[] data, Pointer p, bool includeSubComponents)
        {
            if (!includeSubComponents)
            {
                return;
            }
            lock (m_Components)
            {
                int count = BitPacker.GetInt(data, p);
                for (int i = 0; i < count; i++)
                {
                    uint typeHash = BitPacker.GetUInt(data, p);
                    IComponent wisp = Factory.Instance.CreateObject(typeHash) as IComponent;
                    if(wisp == null)
                    {
                        throw new ArgumentException("Error deserializing wisp object.  Did you remember to register the Wisp Object's ISerializableWispObject.TypeHash with the Factory? Try: |> Factory.Instance.Register(typeof(YourCharacterComponentClassName), () => { return new YourCharacterComponentClassName(); });|");
                    }
                    if (wisp is IComponent)
                    {
                        AddComponent(wisp as IComponent);
                    }
                    wisp.Deserialize(data, p, includeSubComponents);
                }
            }
        }

        public virtual void Serialize(ref byte[] buffer, Pointer p)
        {
            Serialize(ref buffer, p, true);
        }

        public virtual void Deserialize(byte[] data, Pointer p)
        {
            Deserialize(data, p, true);
        }

        public abstract uint TypeHash 
        {
            get;
        }       

    }

    public interface ISerializableWispObject
    {
        uint TypeHash{get;}
        void Serialize(ref byte[] buffer, Pointer p);
        void Deserialize(byte[] data, Pointer p);        
    }
}
