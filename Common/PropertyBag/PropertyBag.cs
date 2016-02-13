using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Runtime.ConstrainedExecution;

namespace Shared
{
    /// <summary>
    /// Stores a collection of arbitrary, mixed and strongly typed values
    /// </summary>
    public class PropertyBag
    {
        /// <summary>
        /// The property bag will use a linked list for storing the properties until it becomes more efficient to use a dictionary/hashtable.
        /// This value indicates what the maximum number of properties is before we switch over to dictionary storage
        /// </summary>
        public static int SmallSizeLimit = 25;

        // Property storage ////////////
        private System.Collections.Generic.Dictionary<int, Property> m_HashedProperties;
        private LinkedList<Property> m_LinkedProperties;
        private bool m_UsingLinkedStorage = true;
        private List<IPropertyBagOwner> m_Listeners = new List<IPropertyBagOwner>();
        private LinkedList<LinkedList<Property>> m_PropertyKindMaps = new LinkedList<LinkedList<Property>>();
        ///////////////////////////////
        
#if !SILVERLIGHT && !UNITY

        public static unsafe int GetPropertyHashCode(string name)
        {
            fixed (char* str = name)
            {
                char* chPtr = str;
                int num = 352654597;
                int num2 = num;
                int* numPtr = (int*)chPtr;
                for (int i = name.Length; i > 0; i -= 4)
                {
                    num = (((num << 5) + num) + (num >> 27)) ^ numPtr[0];
                    if (i <= 2)
                    {
                        break;
                    }
                    num2 = (((num2 << 5) + num2) + (num2 >> 27)) ^ numPtr[1];
                    numPtr += 2;
                }
                return (num + (num2 * 1566083941));
            }
        }
#else
   public static int GetPropertyHashCode(string name)
        {
            var bytes = Encoding.Unicode.GetBytes(name + "\0"); //HACK: need null terminator for odd counts 
            int num = 0x15051505;
            int num2 = num;
            int j = 0;
            for (int i = name.Length; i > 0; i -= 4)
            {
                num = (((num << 5) + num) + (num >> 27)) ^ BitConverter.ToInt32(bytes, j + 0);
                if (i <= 2) break;
                num2 = (((num2 << 5) + num2) + (num2 >> 27)) ^ BitConverter.ToInt32(bytes, j + 4);
                j += 8;
            }
            return (num + (num2 * 0x5D588B65));
        } 
#endif
        public int GetNextAvailablePropertyId(string propertyName)
        {
            return GetPropertyHashCode(propertyName);
        }

        /// <summary>
        /// Updates or adds the properties in @props to in this property bag
        /// </summary>
        /// <param name="props"></param>
        public void UpdateWithValues(PropertyBag propsToUpdate)
        {
            if (propsToUpdate == null)
            {
                return;
            }

            UpdateWithValues(propsToUpdate.AllProperties);
        }

        /// <summary>
        /// Updates or adds the properties in @props to in this property bag
        /// </summary>
        /// <param name="props"></param>
        public void UpdateWithValues(Property[] propsToUpdate)
        {
            if (propsToUpdate == null)
            {
                return;
            }

            for (int i = 0; i < propsToUpdate.Length; i++)
            {
                Property p = Property.Clone(propsToUpdate[i]);
                p.Owner = this;
                AddProperty(p);
            }
        }

        /// <summary>
        /// A name for this property bag.  
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// A GUID name for this property bag.  Use this to keep multiple bags on the game object apart from one another
        /// </summary>
        public Guid ID { get; set; }

        /// <summary>
        /// The number of properties in the bag
        /// </summary>
        public int PropertyCount
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
        public Property[] AllProperties
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
        public PropertyBag(string description)
        {
            ID = Guid.NewGuid();
            Name = description;
            if (SmallSizeLimit > 0)
            {
                m_LinkedProperties = new LinkedList<Property>();
                m_UsingLinkedStorage = true;
            }
            else
            {
                m_UsingLinkedStorage = false;
                m_HashedProperties = new Dictionary<int, Property>();
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public PropertyBag() : this("")
        {
        }

        /// <summary>
        /// Returns a list of all property IDs that are registered as the given property kind.  I.e. give me all 
        /// of the propert IDs in this bag that are of type String, etc
        /// </summary>
        /// <param name="kind"></param>
        /// <returns></returns>
        public List<int> GetAllPropertyTypesOfKind(PropertyKind kind)
        {
            LinkedList<Property> props = GetAllPropertiesOfKind(kind);
            List<int> ids = new List<int>();
            LinkedList<Property>.Enumerator enu = props.GetEnumerator();
            while(enu.MoveNext())
            {
                ids.Add(enu.Current.PropertyId);
            }

            return ids;
        }

        /// <summary>
        /// Gets all of the properties of a certain kind
        /// </summary>
        /// <param name="kind"></param>
        /// <returns></returns>
        public LinkedList<Property> GetAllPropertiesOfKind(PropertyKind kind)
        {
            LinkedList<Property> map = null;
            map = m_PropertyKindMaps.FirstOrDefault(list => list.First.Value.PropertyType == (int)kind);
            if (map == null)
            {
                map = new LinkedList<Property>();
            }
            return map;
        }

        /// <summary>
        /// Retrieves a property from the PropertyBag based on 
        /// the property id
        /// </summary>
        public Property GetProperty(int propertyId)
        {
            // An instance of the Property that will be returned
            Property objProperty = null;

            // If the PropertyBag already contains a property whose name matches
            // the property required, ...
            if (m_UsingLinkedStorage)
            {
                objProperty = m_LinkedProperties.FirstOrDefault(s => s.PropertyId == propertyId);
                if(objProperty != null)
                {
                        // Optimization: move element to beginning of list for optimization.  basically the most requested properties will be
                        // at the beginning of the list, eventually
                        m_LinkedProperties.Remove(objProperty);
                        m_LinkedProperties.AddFirst(objProperty);
                }
            }
            else
            {
                m_HashedProperties.TryGetValue(propertyId, out objProperty);
            }

            return objProperty;
        }

        /// <summary>
        /// Retrieves a property from the PropertyBag based on 
        /// the property id
        /// </summary>
        public Property GetProperty(string propertyName)
        {
            // An instance of the Property that will be returned
            Property objProperty = null;

            // If the PropertyBag already contains a property whose name matches
            // the property required, ...
            if (m_UsingLinkedStorage)
            {
                objProperty = m_LinkedProperties.FirstOrDefault(s => s.Name == propertyName);
                if (objProperty != null)
                {
                    // Optimization: move element to beginning of list for optimization.  basically the most requested properties will be
                    // at the beginning of the list, eventually
                    m_LinkedProperties.Remove(objProperty);
                    m_LinkedProperties.AddFirst(objProperty);
                }
            }
            else
            {
                KeyValuePair<int, Property>? r = m_HashedProperties.FirstOrDefault(s => s.Value.Name == propertyName);
                if (r != null)
                {
                    return ((KeyValuePair<int, Property>)r).Value;
                }
            }

            return objProperty;
        }

        /// <summary>
        /// Local properties don't get serialized (and therefore are not sent across the network).
        /// </summary>
        /// <param name="flag"></param>
        public void SetLocalFlag(string propId, bool flag)
        {
            Property prop = GetProperty(propId);
            if (prop != null)
            {
                prop.IsLocalOnly = flag;
            }
        }

        /// <summary>
        /// Local properties don't get serialized (and therefore are not sent across the network).
        /// </summary>
        /// <param name="flag"></param>
        public void SetLocalFlag(int propId, bool flag)
        {
            Property prop = GetProperty(propId);
            if (prop != null)
            {
                prop.IsLocalOnly = flag;
            }
        }
        
        /// <summary>
        /// Adds a property object to the property bag
        /// </summary>
        /// <param name="objProperty">the property to add</param>
        public void AddProperty(Property objProperty)
        {
            try
            {
                Property current = GetProperty(objProperty.PropertyId); // see if the Id is already registered
                if (current != null && current.Name != objProperty.Name)
                {
                    Log.LogMsg("ERROR UPDATING PROPERTY BAG.  PROPERTY NAME COLLISION! " + objProperty.Name + " :: " + current.Name);
                    return;
                }

                bool wasAdded = current == null;

                if (current != null)
                {
                    //Log.LogMsg("Property id [" + objProperty.PropertyId.ToString() + "] is already registered in property bag. Replacing the old one.");
                    if (m_HashedProperties != null)
                    {
                        m_HashedProperties.Remove(objProperty.PropertyId);
                    }
                    else
                    {
                        m_LinkedProperties.Remove(current);
                    }
                }

                if (objProperty.Name.Length > 0)// see if the name is registered already
                {
                    Property currentByName = GetProperty(objProperty.Name);
                    if (currentByName != null)
                    {
                        //Log.LogMsg("Property name [" + objProperty.Name + "] is already registered in property bag. Replacing the old one.");
                        if (m_HashedProperties != null)
                        {
                            m_HashedProperties.Remove(objProperty.PropertyId);
                        }
                        else
                        {
                            m_LinkedProperties.Remove(currentByName);
                        }
                    }
                }

                int propCount = PropertyCount;
                if (propCount + 1 > PropertyBag.SmallSizeLimit && m_UsingLinkedStorage)
                {
                    m_UsingLinkedStorage = false;

                    // time to switch to hashtable lookups.  move the data over
                    m_HashedProperties = new Dictionary<int, Property>();
                    LinkedList<Property>.Enumerator enu = m_LinkedProperties.GetEnumerator();
                    while (enu.MoveNext())
                    {
                        m_HashedProperties.Add(enu.Current.PropertyId, enu.Current);
                    }

                    m_LinkedProperties.Clear();
                    m_LinkedProperties = null;
                }

                if (m_UsingLinkedStorage)
                {
                    m_LinkedProperties.AddLast(objProperty);
                }
                else
                {
                    m_HashedProperties.Add(objProperty.PropertyId, objProperty);
                }

                objProperty.Owner = this;

                // add it to the map
                LinkedList<Property> map = null;
                map = m_PropertyKindMaps.FirstOrDefault(list => list.First.Value.PropertyType == objProperty.PropertyType);
                if (map == null)
                {
                    map = new LinkedList<Property>();
                    m_PropertyKindMaps.AddLast(map);
                }
                else if (current != null)
                {
                    map.Remove(current);
                }

                if (current != null)
                {
                    current.Owner = null;
                }

                map.AddLast(objProperty);

                if (wasAdded)
                {
                    NotifyPropertyAdded(objProperty);
                }
                else
                {
                    NotifyPropertyUpdated(objProperty);
                }
            }
            catch(Exception e)
            {
                Log.LogMsg("Failed to add Property [" + objProperty.ToString() + "] to property bag [" + Name + "]");
            }
        }

        /// <summary>
        /// Start listening to property change notifications on this bag
        /// </summary>
        /// <param name="listener">the object to receive the notifications</param>
        public void SubscribeToChangeNotifications(IPropertyBagOwner listener)
        {
            m_Listeners.Remove(listener);
            m_Listeners.Add(listener);
        }

        /// <summary>
        /// Stop listening to property change notifications on this bag
        /// </summary>
        /// <param name="listener">the object to no longer receive notifications</param>
        public void UnSubscribeToChangeNotifications(IPropertyBagOwner listener)
        {
            m_Listeners.Remove(listener);
        }

        /// <summary>
        /// Sends out notifications that a property has been updated
        /// </summary>
        /// <param name="p">the property that was updated</param>
        public void NotifyPropertyUpdated(Property p)
        {
            for (int i = 0; i < m_Listeners.Count; i++)
            {
                m_Listeners[i].OnPropertyUpdated(this.ID, p);
            }
        }

        /// <summary>
        /// Sends out notifications that a property has been added
        /// </summary>
        /// <param name="p">the property that was updated</param>
        public void NotifyPropertyAdded(Property p)
        {
            for (int i = 0; i < m_Listeners.Count; i++)
            {
                m_Listeners[i].OnPropertyAdded(this.ID, p);
            }
        }

        /// <summary>
        /// Sends out notifications that a property has been removed
        /// </summary>
        /// <param name="p">the property that was updated</param>
        public void NotifyPropertyRemoved(Property p)
        {
            for (int i = 0; i < m_Listeners.Count; i++)
            {
                m_Listeners[i].OnPropertyRemoved(this.ID, p);
            }
        }

        public void Serialize(ref byte[] buffer, Pointer p)
        {
            Property[] props = AllProperties;
            BitPacker.AddString(ref buffer, p, Name);
            BitPacker.AddString(ref buffer, p, ID.ToString());

            BitPacker.AddInt(ref buffer, p, props.Count(itm => itm.IsLocalOnly == false));
            for (int i = 0; i < props.Length; i++)
            {
                if (props[i].IsLocalOnly)
                {
                    continue;
                }

                BitPacker.AddProperty(ref buffer, p, props[i]);
            }
        }

        public void Deserialize(byte[] buffer, Pointer p)
        {
            Name = BitPacker.GetString(buffer, p);
            ID = new Guid(BitPacker.GetString(buffer, p));

            int num = BitPacker.GetInt(buffer, p);
            for (int i = 0; i < num; i++)
            {
               AddProperty(BitPacker.GetProperty(buffer, p, this));
            }
        }

        #region Property Getters and Setters

        // Int16
        public short? GetShortProperty(int propertyId)
        {
            IPropertyBagProperty<short> prop = GetProperty(propertyId) as Int16Property;
            if (prop == null)
            {
                return null;
            }

            return prop.Value;
        }

        public short? GetShortProperty(string propertyName)
        {
            IPropertyBagProperty<short> prop = GetProperty(propertyName) as Int16Property;
            if (prop == null)
            {
                return null;
            }

            return prop.Value;
        }

        public void SetProperty(int propertyId, ISerializableWispObject val)
        {
            SetProperty("", propertyId, val);
        }

        public void SetProperty(string propertyName, int propertyId, ISerializableWispObject val)
        {
            Property p = GetProperty(propertyId);
            if (p != null && !(p is WispProperty))
            {
                // Property was previously registered with this id, but under a different datatype.  Remove the old one.
                RemoveProperty(p);
            }

            IPropertyBagProperty<ISerializableWispObject> prop = p as WispProperty;
            if (prop == null)
            {
                WispProperty newProp = new WispProperty(propertyName, propertyId, val, this);
                AddProperty(newProp);
                prop = newProp;
            }

            if (val == null)
            {
                RemoveProperty(prop as Property);
                return;
            }

            prop.Value = val;
        }

        public void SetProperty(string propertyName, ISerializableWispObject val)
        {
            Property p = GetProperty(propertyName);
            if (p != null && !(p is WispProperty))
            {
                // Property was previously registered with this id, but under a different datatype.  Remove the old one.
                RemoveProperty(p);
            }

            IPropertyBagProperty<ISerializableWispObject> prop = p as WispProperty;
            if (prop == null)
            {
                WispProperty newProp = new WispProperty(propertyName, GetNextAvailablePropertyId(propertyName), val, this);
                AddProperty(newProp);
                prop = newProp;
            }

            if (val == null)
            {
                RemoveProperty(prop as Property);
                return;
            }

            prop.Value = val;
        }

        public void SetProperty(int propertyId, ISerializableWispObject[] val)
        {
            SetProperty("", propertyId, val);
        }

        public void SetProperty(string propertyName, int propertyId, ISerializableWispObject[] val)
        {
            Property p = GetProperty(propertyId);
            if (p != null && !(p is WispArrayProperty))
            {
                // Property was previously registered with this id, but under a different datatype.  Remove the old one.
                RemoveProperty(p);
            }

            IPropertyBagProperty<ISerializableWispObject[]> prop = p as WispArrayProperty;
            if (prop == null)
            {
                WispArrayProperty newProp = new WispArrayProperty(propertyName, propertyId, val, this);
                AddProperty(newProp);
                prop = newProp;
            }

            if (val == null)
            {
                RemoveProperty(prop as Property);
                return;
            }

            prop.Value = val;
        }

        public void SetProperty(string propertyName, ISerializableWispObject[] val)
        {
            Property p = GetProperty(propertyName);
            if (p != null && !(p is WispArrayProperty))
            {
                // Property was previously registered with this id, but under a different datatype.  Remove the old one.
                RemoveProperty(p);
            }

            IPropertyBagProperty<ISerializableWispObject[]> prop = p as WispArrayProperty;
            if (prop == null)
            {
                WispArrayProperty newProp = new WispArrayProperty(propertyName, GetNextAvailablePropertyId(propertyName), val, this);
                AddProperty(newProp);
                prop = newProp;
            }

            if (val == null)
            {
                RemoveProperty(prop as Property);
                return;
            }

            prop.Value = val;
        }

        public void SetProperty(int propertyId, short val)
        {
            SetProperty("", propertyId, val);
        }

        public void SetProperty(string propertyName, int propertyId, short val)
        {
            Property p = GetProperty(propertyId);
            if (p != null && !(p is Int16Property))
            {
                // Property was previously registered with this id, but under a different datatype.  Remove the old one.
                RemoveProperty(p);
            }

            IPropertyBagProperty<short> prop = p as Int16Property;           
            if (prop == null)
            {
                Int16Property newProp = new Int16Property(propertyName, propertyId, val, this);
                AddProperty(newProp);
                prop = newProp;
            }

            prop.Value = val;
        }

        public void SetProperty(string propertyName, short val)
        {
            Property p = GetProperty(propertyName);
            if (p != null && !(p is Int16Property))
            {
                // Property was previously registered with this id, but under a different datatype.  Remove the old one.
                RemoveProperty(p);
            }

            IPropertyBagProperty<short> prop = p as Int16Property;
            if (prop == null)
            {
                Int16Property newProp = new Int16Property(propertyName, GetNextAvailablePropertyId(propertyName), val, this);
                AddProperty(newProp);
                prop = newProp;
            }

            prop.Value = val;
        }

        public short[] GetShortArrayProperty(int propertyId)
        {
            IPropertyBagProperty<short[]> prop = GetProperty(propertyId) as Int16ArrayProperty;
            if (prop == null)
            {
                return new short[0];
            }

            return prop.Value;
        }

        public short[] GetShortArrayProperty(string propertyName)
        {
            IPropertyBagProperty<short[]> prop = GetProperty(propertyName) as Int16ArrayProperty;
            if (prop == null)
            {
                return new short[0];
            }

            return prop.Value;
        }

        public ISerializableWispObject[] GetWispArrayProperty(int propertyId)
        {
            IPropertyBagProperty<ISerializableWispObject[]> prop = GetProperty(propertyId) as WispArrayProperty;
            if (prop == null)
            {
                return new ISerializableWispObject[0];
            }

            return prop.Value;
        }

        public ISerializableWispObject[] GetWispArrayProperty(string propertyName)
        {
            IPropertyBagProperty<ISerializableWispObject[]> prop = GetProperty(propertyName) as WispArrayProperty;
            if (prop == null)
            {
                return new ISerializableWispObject[0];
            }

            return prop.Value;
        }

        public void SetProperty(int propertyId, short[] val)
        {
            SetProperty("", propertyId, val);
        }

        public void SetProperty(string propertyName, int propertyId, short[] val)
        {
            Property p = GetProperty(propertyId);
            if (p != null && !(p is Int16ArrayProperty))
            {
                // Property was previously registered with this id, but under a different datatype.  Remove the old one.
                RemoveProperty(p);
            }

            IPropertyBagProperty<short[]> prop = p as Int16ArrayProperty;
            if (prop == null)
            {
                Int16ArrayProperty newProp = new Int16ArrayProperty(propertyName, propertyId, val, this);
                AddProperty(newProp);
                prop = newProp;
            }

            if (val == null)
            {
                RemoveProperty(prop as Property);
                return;
            }

            prop.Value = val;
        }

        public void SetProperty(string propertyName, short[] val)
        {
            Property p = GetProperty(propertyName);
            if (p != null && !(p is Int16ArrayProperty))
            {
                // Property was previously registered with this id, but under a different datatype.  Remove the old one.
                RemoveProperty(p);
            }

            IPropertyBagProperty<short[]> prop = p as Int16ArrayProperty;
            if (prop == null)
            {
                Int16ArrayProperty newProp = new Int16ArrayProperty(propertyName, GetNextAvailablePropertyId(propertyName), val, this);
                AddProperty(newProp);
                prop = newProp;
            }

            if (val == null)
            {
                RemoveProperty(prop as Property);
                return;
            }

            prop.Value = val;
        }

        public ISerializableWispObject GetWispProperty(int propertyId)
        {
            IPropertyBagProperty<ISerializableWispObject> prop = GetProperty(propertyId) as WispProperty;
            if (prop == null)
            {
                return null;
            }

            return prop.Value;
        }

        public ISerializableWispObject GetWispProperty(string propertyName)
        {
            IPropertyBagProperty<ISerializableWispObject> prop = GetProperty(propertyName) as WispProperty;
            if (prop == null)
            {
                return null;
            }

            return prop.Value;
        }

        // Guid
        public Guid GetGuidProperty(int propertyId)
        {
            IPropertyBagProperty<Guid> prop = GetProperty(propertyId) as GuidProperty;
            if (prop == null)
            {
                return Guid.Empty;
            }

            return prop.Value;
        }

        public Guid GetGuidProperty(string propertyName)
        {
            IPropertyBagProperty<Guid> prop = GetProperty(propertyName) as GuidProperty;
            if (prop == null)
            {
                return Guid.Empty;
            }

            return prop.Value;
        }

        public void SetProperty(int propertyId, Guid val)
        {
            SetProperty("", propertyId, val);
        }

        public void SetProperty(string propertyName, int propertyId, Guid val)
        {
            Property p = GetProperty(propertyId);
            if (p != null && !(p is GuidProperty))
            {
                // Property was previously registered with this id, but under a different datatype.  Remove the old one.
                RemoveProperty(p);
            }

            IPropertyBagProperty<Guid> prop = p as GuidProperty;
            if (prop == null)
            {
                GuidProperty newProp = new GuidProperty(propertyName, propertyId, val, this);
                AddProperty(newProp);
                prop = newProp;
            }

            prop.Value = val;
        }

        public void SetProperty(string propertyName, Guid val)
        {
            Property p = GetProperty(propertyName);
            if (p != null && !(p is GuidProperty))
            {
                // Property was previously registered with this id, but under a different datatype.  Remove the old one.
                RemoveProperty(p);
            }

            IPropertyBagProperty<Guid> prop = p as GuidProperty;
            if (prop == null)
            {
                GuidProperty newProp = new GuidProperty(propertyName, GetNextAvailablePropertyId(propertyName), val, this);
                AddProperty(newProp);
                prop = newProp;
            }

            prop.Value = val;
        }

        public Guid[] GetGuidArrayProperty(int propertyId)
        {
            IPropertyBagProperty<Guid[]> prop = GetProperty(propertyId) as GuidArrayProperty;
            if (prop == null)
            {
                return new Guid[0];
            }

            return prop.Value;
        }

        public Guid[] GetGuidArrayProperty(string propertyName)
        {
            IPropertyBagProperty<Guid[]> prop = GetProperty(propertyName) as GuidArrayProperty;
            if (prop == null)
            {
                return new Guid[0];
            }

            return prop.Value;
        }

        public void SetProperty(int propertyId, Guid[] val)
        {
            SetProperty("", propertyId, val);
        }

        public void SetProperty(string propertyName, int propertyId, Guid[] val)
        {
            Property p = GetProperty(propertyId);
            if (p != null && !(p is GuidArrayProperty))
            {
                // Property was previously registered with this id, but under a different datatype.  Remove the old one.
                RemoveProperty(p);
            }

            IPropertyBagProperty<Guid[]> prop = p as GuidArrayProperty;
            if (prop == null)
            {
                GuidArrayProperty newProp = new GuidArrayProperty(propertyName, propertyId, val, this);
                AddProperty(newProp);
                prop = newProp;
            }

            if (val == null)
            {
                RemoveProperty(prop as Property);
                return;
            }

            prop.Value = val;
        }

        public void SetProperty(string propertyName, Guid[] val)
        {
            Property p = GetProperty(propertyName);
            if (p != null && !(p is GuidArrayProperty))
            {
                // Property was previously registered with this id, but under a different datatype.  Remove the old one.
                RemoveProperty(p);
            }

            IPropertyBagProperty<Guid[]> prop = p as GuidArrayProperty;
            if (prop == null)
            {
                GuidArrayProperty newProp = new GuidArrayProperty(propertyName, GetNextAvailablePropertyId(propertyName), val, this);
                AddProperty(newProp);
                prop = newProp;
            }

            if (val == null)
            {
                RemoveProperty(prop as Property);
                return;
            }

            prop.Value = val;
        }

        // Int32
        public int? GetIntProperty(int propertyId)
        {
            IPropertyBagProperty<int> prop = GetProperty(propertyId) as Int32Property;
            if (prop == null)
            {
                return null;
            }

            return prop.Value;
        }

        public int? GetIntProperty(string propertyName)
        {
            IPropertyBagProperty<int> prop = GetProperty(propertyName) as Int32Property;
            if (prop == null)
            {
                return null;
            }

            return prop.Value;
        }

        public void SetProperty(int propertyId, int val)
        {
            SetProperty("", propertyId, val);
        }

        public void SetProperty(string propertyName, int propertyId, int val)
        {
            Property p = GetProperty(propertyId);
            if(p != null && !(p is Int32Property))
            {
                // Property was previously registered with this id, but under a different datatype.  Remove the old one.
                RemoveProperty(p);
            }
            IPropertyBagProperty<int> prop = p as Int32Property;
            if (prop == null)
            {
                Int32Property newProp = new Int32Property(propertyName, propertyId, val, this);
                AddProperty(newProp);
                prop = newProp;
            }

            prop.Value = val;
        }

        public void SetProperty(string propertyName, int val)
        {
            Property p = GetProperty(propertyName);
            if (p != null && !(p is Int32Property))
            {
                // Property was previously registered with this id, but under a different datatype.  Remove the old one.
                RemoveProperty(p);
            }

            IPropertyBagProperty<int> prop = p as Int32Property;
            if (prop == null)
            {
                Int32Property newProp = new Int32Property(propertyName, GetNextAvailablePropertyId(propertyName), val, this);
                AddProperty(newProp);
                prop = newProp;
            }

            prop.Value = val;
        }
                
                
        public int[] GetIntArrayProperty(int propertyId)
        {
            Property p = GetProperty(propertyId);
            if (p != null && !(p is Int32ArrayProperty))
            {
                // Property was previously registered with this id, but under a different datatype.  Remove the old one.
                RemoveProperty(p);
            }

            IPropertyBagProperty<int[]> prop = p as Int32ArrayProperty;
            if (prop == null)
            {
                return new int[0];
            }

            return prop.Value;
        }

        public int[] GetIntArrayProperty(string propertyName)
        {
            Property p = GetProperty(propertyName);
            if (p != null && !(p is Int32ArrayProperty))
            {
                // Property was previously registered with this id, but under a different datatype.  Remove the old one.
                RemoveProperty(p);
            }

            IPropertyBagProperty<int[]> prop = p as Int32ArrayProperty;
            if (prop == null)
            {
                return new int[0];
            }

            return prop.Value;
        }

        public void SetProperty(int propertyId, int[] val)
        {
            SetProperty("", propertyId, val);
        }

        public void SetProperty(string propertyName, int propertyId, int[] val)
        {
            Property p = GetProperty(propertyId);
            if (p != null && !(p is Int32ArrayProperty))
            {
                // Property was previously registered with this id, but under a different datatype.  Remove the old one.
                RemoveProperty(p);
            }

            IPropertyBagProperty<int[]> prop = p as Int32ArrayProperty;
            if (prop == null)
            {
                Int32ArrayProperty newProp = new Int32ArrayProperty(propertyName, propertyId, val, this);
                AddProperty(newProp);
                prop = newProp;
            }

            if (val == null)
            {
                RemoveProperty(prop as Property);
                return;
            }

            prop.Value = val;
        }

        public void SetProperty(string propertyName, int[] val)
        {
            Property p = GetProperty(propertyName);
            if (p != null && !(p is Int32ArrayProperty))
            {
                // Property was previously registered with this id, but under a different datatype.  Remove the old one.
                RemoveProperty(p);
            }

            IPropertyBagProperty<int[]> prop = p as Int32ArrayProperty;
            if (prop == null)
            {
                Int32ArrayProperty newProp = new Int32ArrayProperty(propertyName, GetNextAvailablePropertyId(propertyName), val, this);
                AddProperty(newProp);
                prop = newProp;
            }

            if (val == null)
            {
                RemoveProperty(prop as Property);
                return;
            }

            prop.Value = val;
        }

        // Int64
        public long? GetLongProperty(int propertyId)
        {
            IPropertyBagProperty<long> prop = GetProperty(propertyId) as Int64Property;
            if (prop == null)
            {
                return null;
            }

            return prop.Value;
        }

        public long? GetLongProperty(string propertyName)
        {
            IPropertyBagProperty<long> prop = GetProperty(propertyName) as Int64Property;
            if (prop == null)
            {
                return null;
            }

            return prop.Value;
        }

        public void SetProperty(int propertyId, long val)
        {
            SetProperty("", propertyId, val); ;
        }

        public void SetProperty(string propertyName, int propertyId, long val)
        {
            Property p = GetProperty(propertyId);
            if (p != null && !(p is Int64Property))
            {
                // Property was previously registered with this id, but under a different datatype.  Remove the old one.
                RemoveProperty(p);
            }

            IPropertyBagProperty<long> prop = p as Int64Property;
            if (prop == null)
            {
                Int64Property newProp = new Int64Property(propertyName, propertyId, val, this);
                AddProperty(newProp);
                prop = newProp;
            }

            prop.Value = val;
        }

        public void SetProperty(string propertyName, long val)
        {
            Property p = GetProperty(propertyName);
            if (p != null && !(p is Int64Property))
            {
                // Property was previously registered with this id, but under a different datatype.  Remove the old one.
                RemoveProperty(p);
            }

            IPropertyBagProperty<long> prop = p as Int64Property;
            if (prop == null)
            {
                Int64Property newProp = new Int64Property(propertyName, GetNextAvailablePropertyId(propertyName), val, this);
                AddProperty(newProp);
                prop = newProp;
            }

            prop.Value = val;
        }

        public long[] GetLongArrayProperty(int propertyId)
        {
            IPropertyBagProperty<long[]> prop = GetProperty(propertyId) as Int64ArrayProperty;
            if (prop == null)
            {
                return new long[0];
            }

            return prop.Value;
        }

        public long[] GetLongArrayProperty(string propertyName)
        {
            IPropertyBagProperty<long[]> prop = GetProperty(propertyName) as Int64ArrayProperty;
            if (prop == null)
            {
                return new long[0];
            }

            return prop.Value;
        }

        public void SetProperty(int propertyId, long[] val)
        {
            SetProperty("", propertyId, val); ;
        }

        public void SetProperty(string propertyName, int propertyId, long[] val)
        {
            Property p = GetProperty(propertyId);
            if (p != null && !(p is Int64ArrayProperty))
            {
                // Property was previously registered with this id, but under a different datatype.  Remove the old one.
                RemoveProperty(p);
            }

            IPropertyBagProperty<long[]> prop = p as Int64ArrayProperty;
            if (prop == null)
            {
                Int64ArrayProperty newProp = new Int64ArrayProperty(propertyName, propertyId, val, this);
                AddProperty(newProp);
                prop = newProp;
            }

            if (val == null)
            {
                RemoveProperty(prop as Property);
                return;
            }

            prop.Value = val;
        }

        public void SetProperty(string propertyName, long[] val)
        {
            Property p = GetProperty(propertyName);
            if (p != null && !(p is Int64ArrayProperty))
            {
                // Property was previously registered with this id, but under a different datatype.  Remove the old one.
                RemoveProperty(p);
            }

            IPropertyBagProperty<long[]> prop = p as Int64ArrayProperty;
            if (prop == null)
            {
                Int64ArrayProperty newProp = new Int64ArrayProperty(propertyName, GetNextAvailablePropertyId(propertyName), val, this);
                AddProperty(newProp);
                prop = newProp;
            }

            if (val == null)
            {
                RemoveProperty(prop as Property);
                return;
            }

            prop.Value = val;
        }

        // String
        public string GetStringProperty(int propertyId)
        {
            IPropertyBagProperty<string> prop = GetProperty(propertyId) as StringProperty;
            if (prop == null)
            {
                return "";
            }

            return prop.Value;
        }

        public string GetStringProperty(string propertyName)
        {
            IPropertyBagProperty<string> prop = GetProperty(propertyName) as StringProperty;
            if (prop == null)
            {
                return "";
            }

            return prop.Value;
        }

        public void SetProperty(int propertyId, string val)
        {
            SetProperty("", propertyId, val); ;
        }

        public void SetProperty(string propertyName, int propertyId, string val)
        {
            Property p = GetProperty(propertyId);
            if (p != null && !(p is StringProperty))
            {
                // Property was previously registered with this id, but under a different datatype.  Remove the old one.
                RemoveProperty(p);
            }

            IPropertyBagProperty<string> prop = p as StringProperty;
            if (prop == null)
            {
                StringProperty newProp = new StringProperty(propertyName, propertyId, val, this);
                AddProperty(newProp);
                prop = newProp;
            }

            if (val == null)
            {
                RemoveProperty(prop as Property);
                return;
            }

            prop.Value = val;
        }

        public void SetProperty(string propertyName, string val)
        {
            Property p = GetProperty(propertyName);
            if (p != null && !(p is StringProperty))
            {
                // Property was previously registered with this id, but under a different datatype.  Remove the old one.
                RemoveProperty(p);
            }

            IPropertyBagProperty<string> prop = p as StringProperty;
            if (prop == null)
            {
                StringProperty newProp = new StringProperty(propertyName, GetNextAvailablePropertyId(propertyName), val, this);
                AddProperty(newProp);
                prop = newProp;
            }

            if (val == null)
            {
                RemoveProperty(prop as Property);
                return;
            }

            prop.Value = val;
        }

        public string[] GetStringArrayProperty(int propertyId)
        {
            IPropertyBagProperty<string[]> prop = GetProperty(propertyId) as StringArrayProperty;
            if (prop == null)
            {
                return new string[0];
            }

            return prop.Value;
        }

        public string[] GetStringArrayProperty(string propertyName)
        {
            IPropertyBagProperty<string[]> prop = GetProperty(propertyName) as StringArrayProperty;
            if (prop == null)
            {
                return new string[0];
            }

            return prop.Value;
        }

        public void SetProperty(int propertyId, string[] val)
        {
            SetProperty("", propertyId, val); ;
        }

        public void SetProperty(string propertyName, int propertyId, string[] val)
        {
            Property p = GetProperty(propertyId);
            if (p != null && !(p is StringArrayProperty))
            {
                // Property was previously registered with this id, but under a different datatype.  Remove the old one.
                RemoveProperty(p);
            }

            IPropertyBagProperty<string[]> prop = p as StringArrayProperty;
            if (prop == null)
            {
                StringArrayProperty newProp = new StringArrayProperty(propertyName, propertyId, val, this);
                AddProperty(newProp);
                prop = newProp;
            }

            if (val == null)
            {
                RemoveProperty(prop as Property);
                return;
            }

            prop.Value = val;
        }

        public void SetProperty(string propertyName, string[] val)
        {
            Property p = GetProperty(propertyName);
            if (p != null && !(p is StringArrayProperty))
            {
                // Property was previously registered with this id, but under a different datatype.  Remove the old one.
                RemoveProperty(p);
            }

            IPropertyBagProperty<string[]> prop = p as StringArrayProperty;
            if (prop == null)
            {
                StringArrayProperty newProp = new StringArrayProperty(propertyName, GetNextAvailablePropertyId(propertyName), val, this);
                AddProperty(newProp);
                prop = newProp;
            }

            if (val == null)
            {
                RemoveProperty(prop as Property);
                return;
            }

            prop.Value = val;
        }


        // Bool
        public bool? GetBoolProperty(int propertyId)
        {
            IPropertyBagProperty<bool> prop = GetProperty(propertyId) as BoolProperty;
            if (prop == null)
            {
                return null;
            }

            return prop.Value;
        }

        public bool? GetBoolProperty(string propertyName)
        {
            IPropertyBagProperty<bool> prop = GetProperty(propertyName) as BoolProperty;
            if (prop == null)
            {
                return null;
            }

            return prop.Value;
        }

        public void SetProperty(int propertyId, bool val)
        {
            SetProperty("", propertyId, val); ;
        }

        public void SetProperty(string propertyName, bool val)
        {
            Property p = GetProperty(propertyName);
            if (p != null && !(p is BoolProperty))
            {
                // Property was previously registered with this id, but under a different datatype.  Remove the old one.
                RemoveProperty(p);
            }

            IPropertyBagProperty<bool> prop = p as BoolProperty;
            if (prop == null)
            {
                BoolProperty newProp = new BoolProperty(propertyName, GetNextAvailablePropertyId(propertyName), val, this);
                AddProperty(newProp);
                prop = newProp;
            }

            prop.Value = val;
        }


        public void SetProperty(string propertyName, int propertyId, bool val)
        {
            Property p = GetProperty(propertyId);
            if (p != null && !(p is BoolProperty))
            {
                // Property was previously registered with this id, but under a different datatype.  Remove the old one.
                RemoveProperty(p);
            }

            IPropertyBagProperty<bool> prop = p as BoolProperty;
            if (prop == null)
            {
                BoolProperty newProp = new BoolProperty(propertyName, propertyId, val, this);
                AddProperty(newProp);
                prop = newProp;
            }

            prop.Value = val;
        }

        public bool[] GetBoolArrayProperty(int propertyId)
        {
            IPropertyBagProperty<bool[]> prop = GetProperty(propertyId) as BoolArrayProperty;
            if (prop == null)
            {
                return new bool[0];
            }

            return prop.Value;
        }

        public bool[] GetBoolArrayProperty(string propertyName)
        {
            IPropertyBagProperty<bool[]> prop = GetProperty(propertyName) as BoolArrayProperty;
            if (prop == null)
            {
                return new bool[0];
            }

            return prop.Value;
        }

        public void SetProperty(int propertyId, bool[] val)
        {
            SetProperty("", propertyId, val); ;
        }

        public void SetProperty(string propertyName, bool[] val)
        {
            Property p = GetProperty(propertyName);
            if (p != null && !(p is BoolArrayProperty))
            {
                // Property was previously registered with this id, but under a different datatype.  Remove the old one.
                RemoveProperty(p);
            }

            IPropertyBagProperty<bool[]> prop = p as BoolArrayProperty;
            if (prop == null)
            {
                BoolArrayProperty newProp = new BoolArrayProperty(propertyName, GetNextAvailablePropertyId(propertyName), val, this);
                AddProperty(newProp);
                prop = newProp;
            }

            if (val == null)
            {
                RemoveProperty(prop as Property);
                return;
            }

            prop.Value = val;
        }

        public void SetProperty(string propertyName, int propertyId, bool[] val)
        {
            Property p = GetProperty(propertyId);
            if (p != null && !(p is BoolArrayProperty))
            {
                // Property was previously registered with this id, but under a different datatype.  Remove the old one.
                RemoveProperty(p);
            }

            IPropertyBagProperty<bool[]> prop = p as BoolArrayProperty;
            if (prop == null)
            {
                BoolArrayProperty newProp = new BoolArrayProperty(propertyName, propertyId, val, this);
                AddProperty(newProp);
                prop = newProp;
            }

            if (val == null)
            {
                RemoveProperty(prop as Property);
                return;
            }

            prop.Value = val;
        }

        // Byte
        public byte? GetByteProperty(int propertyId)
        {
            IPropertyBagProperty<byte> prop = GetProperty(propertyId) as ByteProperty;
            if (prop == null)
            {
                return null;
            }

            return prop.Value;
        }

        public byte? GetByteProperty(string propertyName)
        {
            IPropertyBagProperty<byte> prop = GetProperty(propertyName) as ByteProperty;
            if (prop == null)
            {
                return null;
            }

            return prop.Value;
        }

        public void SetProperty(int propertyId, byte val)
        {
            SetProperty("", propertyId, val); ;
        }

        public void SetProperty(string propertyName, int propertyId, byte val)
        {
            Property p = GetProperty(propertyId);
            if (p != null && !(p is ByteProperty))
            {
                // Property was previously registered with this id, but under a different datatype.  Remove the old one.
                RemoveProperty(p);
            }

            IPropertyBagProperty<byte> prop = p as ByteProperty;
            if (prop == null)
            {
                ByteProperty newProp = new ByteProperty(propertyName, propertyId, val, this);
                AddProperty(newProp);
                prop = newProp;
            }

            prop.Value = val;
        }

        public void SetProperty(string propertyName, byte val)
        {
            Property p = GetProperty(propertyName);
            if (p != null && !(p is ByteProperty))
            {
                // Property was previously registered with this id, but under a different datatype.  Remove the old one.
                RemoveProperty(p);
            }

            IPropertyBagProperty<byte> prop = p as ByteProperty;
            if (prop == null)
            {
                ByteProperty newProp = new ByteProperty(propertyName, GetNextAvailablePropertyId(propertyName), val, this);
                AddProperty(newProp);
                prop = newProp;
            }

            prop.Value = val;
        }


        public byte[] GetByteArrayProperty(int propertyId)
        {
            IPropertyBagProperty<byte[]> prop = GetProperty(propertyId) as ByteArrayProperty;
            if (prop == null)
            {
                return new byte[0];
            }

            return prop.Value;
        }

        public byte[] GetByteArrayProperty(string propertyName)
        {
            IPropertyBagProperty<byte[]> prop = GetProperty(propertyName) as ByteArrayProperty;
            if (prop == null)
            {
                return new byte[0];
            }

            return prop.Value;
        }

        public void SetProperty(int propertyId, byte[] val)
        {
            SetProperty("", propertyId, val); ;
        }

        public void SetProperty(string propertyName, int propertyId, byte[] val)
        {
            Property p = GetProperty(propertyId);
            if (p != null && !(p is ByteArrayProperty))
            {
                // Property was previously registered with this id, but under a different datatype.  Remove the old one.
                RemoveProperty(p);
            }

            IPropertyBagProperty<byte[]> prop = p as ByteArrayProperty;
            if (prop == null)
            {
                ByteArrayProperty newProp = new ByteArrayProperty(propertyName, propertyId, val, this);
                AddProperty(newProp);
                prop = newProp;
            }

            if (val == null)
            {
                RemoveProperty(prop as Property);
                return;
            }

            prop.Value = val;
        }

        public void SetProperty(string propertyName, byte[] val)
        {
            Property p = GetProperty(propertyName);
            if (p != null && !(p is ByteArrayProperty))
            {
                // Property was previously registered with this id, but under a different datatype.  Remove the old one.
                RemoveProperty(p);
            }

            IPropertyBagProperty<byte[]> prop = p as ByteArrayProperty;
            if (prop == null)
            {
                ByteArrayProperty newProp = new ByteArrayProperty(propertyName, GetNextAvailablePropertyId(propertyName), val, this);
                AddProperty(newProp);
                prop = newProp;
            }

            if (val == null)
            {
                RemoveProperty(prop as Property);
                return;
            }

            prop.Value = val;
        }

        // DateTime
        public DateTime? GetDateTimeProperty(int propertyId)
        {
            IPropertyBagProperty<DateTime> prop = GetProperty(propertyId) as DateTimeProperty;
            if (prop == null)
            {
                return null;
            }

            return prop.Value;
        }

        public DateTime? GetDateTimeProperty(string propertyName)
        {
            IPropertyBagProperty<DateTime> prop = GetProperty(propertyName) as DateTimeProperty;
            if (prop == null)
            {
                return null;
            }

            return prop.Value;
        }

        public void SetProperty(int propertyId, DateTime val)
        {
            SetProperty("", propertyId, val); ;
        }

        public void SetProperty(string propertyName, int propertyId, DateTime val)
        {
            Property p = GetProperty(propertyId);
            if (p != null && !(p is DateTimeProperty))
            {
                // Property was previously registered with this id, but under a different datatype.  Remove the old one.
                RemoveProperty(p);
            }

            IPropertyBagProperty<DateTime> prop = p as DateTimeProperty;
            if (prop == null)
            {
                DateTimeProperty newProp = new DateTimeProperty(propertyName, propertyId, val, this);
                AddProperty(newProp);
                prop = newProp;
            }

            prop.Value = val;
        }

        public void SetProperty(string propertyName, DateTime val)
        {
            Property p = GetProperty(propertyName);
            if (p != null && !(p is DateTimeProperty))
            {
                // Property was previously registered with this id, but under a different datatype.  Remove the old one.
                RemoveProperty(p);
            }

            IPropertyBagProperty<DateTime> prop = p as DateTimeProperty;
            if (prop == null)
            {
                DateTimeProperty newProp = new DateTimeProperty(propertyName, GetNextAvailablePropertyId(propertyName), val, this);
                AddProperty(newProp);
                prop = newProp;
            }

            prop.Value = val;
        }

        public DateTime[] GetDateTimeArrayProperty(int propertyId)
        {
            IPropertyBagProperty<DateTime[]> prop = GetProperty(propertyId) as DateTimeArrayProperty;
            if (prop == null)
            {
                return new DateTime[0];
            }

            return prop.Value;
        }

        public DateTime[] GetDateTimeArrayProperty(string propertyName)
        {
            IPropertyBagProperty<DateTime[]> prop = GetProperty(propertyName) as DateTimeArrayProperty;
            if (prop == null)
            {
                return new DateTime[0];
            }

            return prop.Value;
        }

        public void SetProperty(int propertyId, DateTime[] val)
        {
            SetProperty("", propertyId, val); ;
        }

        public void SetProperty(string propertyName, int propertyId, DateTime[] val)
        {
            Property p = GetProperty(propertyId);
            if (p != null && !(p is DateTimeArrayProperty))
            {
                // Property was previously registered with this id, but under a different datatype.  Remove the old one.
                RemoveProperty(p);
            }

            IPropertyBagProperty<DateTime[]> prop = p as DateTimeArrayProperty;
            if (prop == null)
            {
                DateTimeArrayProperty newProp = new DateTimeArrayProperty(propertyName, propertyId, val, this);
                AddProperty(newProp);
                prop = newProp;
            }

            if (val == null)
            {
                RemoveProperty(prop as Property);
                return;
            }

            prop.Value = val;
        }

        public void SetProperty(string propertyName, DateTime[] val)
        {
            Property p = GetProperty(propertyName);
            if (p != null && !(p is DateTimeArrayProperty))
            {
                // Property was previously registered with this id, but under a different datatype.  Remove the old one.
                RemoveProperty(p);
            }

            IPropertyBagProperty<DateTime[]> prop = p as DateTimeArrayProperty;
            if (prop == null)
            {
                DateTimeArrayProperty newProp = new DateTimeArrayProperty(propertyName, GetNextAvailablePropertyId(propertyName), val, this);
                AddProperty(newProp);
                prop = newProp;
            }

            if (val == null)
            {
                RemoveProperty(prop as Property);
                return;
            }

            prop.Value = val;
        }

        // Single
        public float? GetSinglelProperty(int propertyId)
        {
            IPropertyBagProperty<float> prop = GetProperty(propertyId) as SingleProperty;
            if (prop == null)
            {
                return null;
            }

            return prop.Value;
        }

        public float? GetSinglelProperty(string propertyName)
        {
            IPropertyBagProperty<float> prop = GetProperty(propertyName) as SingleProperty;
            if (prop == null)
            {
                return null;
            }

            return prop.Value;
        }

        public void SetProperty(int propertyId, float val)
        {
            SetProperty("", propertyId, val); ;
        }

        public void SetProperty(string propertyName, int propertyId, float val)
        {
            Property p = GetProperty(propertyId);
            if (p != null && !(p is SingleProperty))
            {
                // Property was previously registered with this id, but under a different datatype.  Remove the old one.
                RemoveProperty(p);
            }

            IPropertyBagProperty<float> prop = p as SingleProperty;
            if (prop == null)
            {
                SingleProperty newProp = new SingleProperty(propertyName, propertyId, val, this);
                AddProperty(newProp);
                prop = newProp;
            }

            prop.Value = val;
        }

        public void SetProperty(string propertyName, float val)
        {
            Property p = GetProperty(propertyName);
            if (p != null && !(p is SingleProperty))
            {
                // Property was previously registered with this id, but under a different datatype.  Remove the old one.
                RemoveProperty(p);
            }

            IPropertyBagProperty<float> prop = p as SingleProperty;
            if (prop == null)
            {
                SingleProperty newProp = new SingleProperty(propertyName, GetNextAvailablePropertyId(propertyName), val, this);
                AddProperty(newProp);
                prop = newProp;
            }

            prop.Value = val;
        }

        public float[] GetSingleArrayProperty(int propertyId)
        {
            IPropertyBagProperty<float[]> prop = GetProperty(propertyId) as SingleArrayProperty;
            if (prop == null)
            {
                return new float[0];
            }

            return prop.Value;
        }

        public float[] GetSingleArrayProperty(string propertyName)
        {
            IPropertyBagProperty<float[]> prop = GetProperty(propertyName) as SingleArrayProperty;
            if (prop == null)
            {
                return new float[0];
            }

            return prop.Value;
        }

        public void SetProperty(int propertyId, float[] val)
        {
            SetProperty("", propertyId, val); ;
        }

        public void SetProperty(string propertyName, int propertyId, float[] val)
        {
            Property p = GetProperty(propertyId);
            if (p != null && !(p is SingleArrayProperty))
            {
                // Property was previously registered with this id, but under a different datatype.  Remove the old one.
                RemoveProperty(p);
            }

            IPropertyBagProperty<float[]> prop = p as SingleArrayProperty;
            if (prop == null)
            {
                SingleArrayProperty newProp = new SingleArrayProperty(propertyName, propertyId, val, this);
                AddProperty(newProp);
                prop = newProp;
            }

            if (val == null)
            {
                RemoveProperty(prop as Property);
                return;
            }

            prop.Value = val;
        }

        public void SetProperty(string propertyName, float[] val)
        {
            Property p = GetProperty(propertyName);
            if (p != null && !(p is SingleArrayProperty))
            {
                // Property was previously registered with this id, but under a different datatype.  Remove the old one.
                RemoveProperty(p);
            }

            IPropertyBagProperty<float[]> prop = p as SingleArrayProperty;
            if (prop == null)
            {
                SingleArrayProperty newProp = new SingleArrayProperty(propertyName, GetNextAvailablePropertyId(propertyName), val, this);
                AddProperty(newProp);
                prop = newProp;
            }

            if (val == null)
            {
                RemoveProperty(prop as Property);
                return;
            }

            prop.Value = val;
        }

        // Double
        public double? GetDoubleProperty(int propertyId)
        {
            IPropertyBagProperty<double> prop = GetProperty(propertyId) as DoubleProperty;
            if (prop == null)
            {
                return null;
            }

            return prop.Value;
        }

        public double? GetDoubleProperty(string propertyName)
        {
            IPropertyBagProperty<double> prop = GetProperty(propertyName) as DoubleProperty;
            if (prop == null)
            {
                return null;
            }

            return prop.Value;
        }

        public void SetProperty(int propertyId, double val)
        {
            SetProperty("", propertyId, val); ;
        }

        public void SetProperty(string propertyName, int propertyId, double val)
        {
            Property p = GetProperty(propertyId);
            if (p != null && !(p is DoubleProperty))
            {
                // Property was previously registered with this id, but under a different datatype.  Remove the old one.
                RemoveProperty(p);
            }

            IPropertyBagProperty<double> prop = p as DoubleProperty;
            if (prop == null)
            {
                DoubleProperty newProp = new DoubleProperty(propertyName, propertyId, val, this);
                AddProperty(newProp);
                prop = newProp;
            }

            prop.Value = val;
        }

        public void SetProperty(string propertyName, double val)
        {
            Property p = GetProperty(propertyName);
            if (p != null && !(p is DoubleProperty))
            {
                // Property was previously registered with this id, but under a different datatype.  Remove the old one.
                RemoveProperty(p);
            }

            IPropertyBagProperty<double> prop = p as DoubleProperty;
            if (prop == null)
            {
                DoubleProperty newProp = new DoubleProperty(propertyName, GetNextAvailablePropertyId(propertyName), val, this);
                AddProperty(newProp);
                prop = newProp;
            }

            prop.Value = val;
        }

        public double[] GetDoubleArrayProperty(int propertyId)
        {
            IPropertyBagProperty<double[]> prop = GetProperty(propertyId) as DoubleArrayProperty;
            if (prop == null)
            {
                return new double[0];
            }

            return prop.Value;
        }

        public double[] GetDoubleArrayProperty(string propertyName)
        {
            IPropertyBagProperty<double[]> prop = GetProperty(propertyName) as DoubleArrayProperty;
            if (prop == null)
            {
                return new double[0];
            }

            return prop.Value;
        }

        public void SetProperty(int propertyId, double[] val)
        {
            SetProperty("", propertyId, val); ;
        }

        public void SetProperty(string propertyName, int propertyId, double[] val)
        {
            Property p = GetProperty(propertyId);
            if (p != null && !(p is DoubleArrayProperty))
            {
                // Property was previously registered with this id, but under a different datatype.  Remove the old one.
                RemoveProperty(p);
            }

            IPropertyBagProperty<double[]> prop = p as DoubleArrayProperty;
            if (prop == null)
            {
                DoubleArrayProperty newProp = new DoubleArrayProperty(propertyName, propertyId, val, this);
                AddProperty(newProp);
                prop = newProp;
            }

            if (val == null)
            {
                RemoveProperty(prop as Property);
                return;
            }

            prop.Value = val;
        }

        public void SetProperty(string propertyName, double[] val)
        {
            Property p = GetProperty(propertyName);
            if (p != null && !(p is DoubleArrayProperty))
            {
                // Property was previously registered with this id, but under a different datatype.  Remove the old one.
                RemoveProperty(p);
            }

            IPropertyBagProperty<double[]> prop = p as DoubleArrayProperty;
            if (prop == null)
            {
                DoubleArrayProperty newProp = new DoubleArrayProperty(propertyName, GetNextAvailablePropertyId(propertyName), val, this);
                AddProperty(newProp);
                prop = newProp;
            }

            if (val == null)
            {
                RemoveProperty(prop as Property);
                return;
            }

            prop.Value = val;
        }

        // Stat
        /*
        public Stat GetStatlProperty(int propertyId)
        {
            IPropertyBagProperty<Stat> prop = GetProperty(propertyId) as StatProperty;
            if (prop == null)
            {
                return null;
            }

            return prop.Value;
        }

        public void SetProperty(string propertyName, int propertyId, Stat val)
        {
            IPropertyBagProperty<Stat> prop = GetProperty(propertyId) as StatProperty;
            if (prop == null)
            {
                StatProperty newProp = new StatProperty(propertyName, propertyId, val, this);
                AddProperty(newProp);
                prop = newProp;
            }

            prop.Value = val;
        }

        public Stat[] GetStatArrayProperty(int propertyId)
        {
            IPropertyBagProperty<Stat[]> prop = GetProperty(propertyId) as StatArrayProperty;
            if (prop == null)
            {
                return new Stat[0];
            }

            return prop.Value;
        }

        public void SetProperty(string propertyName, int propertyId, Stat[] val)
        {
            IPropertyBagProperty<Stat[]> prop = GetProperty(propertyId) as StatArrayProperty;
            if (prop == null)
            {
                StatArrayProperty newProp = new StatArrayProperty(propertyName, propertyId, val, this);
                AddProperty(newProp);
                prop = newProp;
            }

            prop.Value = val;
        }
        */
        // Component
        public IComponent GetComponentProperty(int propertyId)
        {
            IPropertyBagProperty<IComponent> prop = GetProperty(propertyId) as ComponentProperty;
            if (prop == null)
            {
                return null;
            }

            return prop.Value;
        }

        public IComponent GetComponentProperty(string propertyName)
        {
            IPropertyBagProperty<IComponent> prop = GetProperty(propertyName) as ComponentProperty;
            if (prop == null)
            {
                return null;
            }

            return prop.Value;
        }


        public void SetProperty(int propertyId, IComponent val)
        {
            SetProperty("", propertyId, val); ;
        }

        public void SetProperty(string propertyName, int propertyId, IComponent val)
        {
            Property p = GetProperty(propertyId);
            if (p != null && !(p is ComponentProperty))
            {
                // Property was previously registered with this id, but under a different datatype.  Remove the old one.
                RemoveProperty(p);
            }

            IPropertyBagProperty<IComponent> prop = p as ComponentProperty;
            if (prop == null)
            {
                ComponentProperty newProp = new ComponentProperty(propertyName, propertyId, val, this);
                AddProperty(newProp);
                prop = newProp;
            }

            if (val == null)
            {
                RemoveProperty(prop as Property);
                return;
            }

            prop.Value = val;
        }

        public void SetProperty(string propertyName, IComponent val)
        {
            Property p = GetProperty(propertyName);
            if (p != null && !(p is ComponentProperty))
            {
                // Property was previously registered with this id, but under a different datatype.  Remove the old one.
                RemoveProperty(p);
            }

            IPropertyBagProperty<IComponent> prop = p as ComponentProperty;
            if (prop == null)
            {
                ComponentProperty newProp = new ComponentProperty(propertyName, GetNextAvailablePropertyId(propertyName), val, this);
                AddProperty(newProp);
                prop = newProp;
            }

            if (val == null)
            {
                RemoveProperty(prop as Property);
                return;
            }

            prop.Value = val;
        }


        public IComponent[] GetComponentArrayProperty(int propertyId)
        {
            IPropertyBagProperty<IComponent[]> prop = GetProperty(propertyId) as ComponentArrayProperty;
            if (prop == null)
            {
                return new IComponent[0];
            }

            return prop.Value;
        }

        public IComponent[] GetComponentArrayProperty(string propertyName)
        {
            IPropertyBagProperty<IComponent[]> prop = GetProperty(propertyName) as ComponentArrayProperty;
            if (prop == null)
            {
                return new IComponent[0];
            }

            return prop.Value;
        }

        public void SetProperty(int propertyId, IComponent[] val)
        {
            SetProperty("", propertyId, val); ;
        }

        public void SetProperty(string propertyName, int propertyId, IComponent[] val)
        {
            Property p = GetProperty(propertyId);
            if (p != null && !(p is ComponentArrayProperty))
            {
                // Property was previously registered with this id, but under a different datatype.  Remove the old one.
                RemoveProperty(p);
            }

            IPropertyBagProperty<IComponent[]> prop = p as ComponentArrayProperty;
            if (prop == null)
            {
                ComponentArrayProperty newProp = new ComponentArrayProperty(propertyName, propertyId, val, this);
                AddProperty(newProp);
                prop = newProp;
            }

            if (val == null)
            {
                RemoveProperty(prop as Property);
                return;
            }

            prop.Value = val;
        }

        public void SetProperty(string propertyName, IComponent[] val)
        {
            Property p = GetProperty(propertyName);
            if (p != null && !(p is ComponentArrayProperty))
            {
                // Property was previously registered with this id, but under a different datatype.  Remove the old one.
                RemoveProperty(p);
            }

            IPropertyBagProperty<IComponent[]> prop = p as ComponentArrayProperty;
            if (prop == null)
            {
                ComponentArrayProperty newProp = new ComponentArrayProperty(propertyName, GetNextAvailablePropertyId(propertyName), val, this);
                AddProperty(newProp);
                prop = newProp;
            }

            if (val == null)
            {
                RemoveProperty(prop as Property);
                return;
            }

            prop.Value = val;
        }

        #endregion        

    
        /// <summary>
        /// Removes all properties in the given statbag
        /// </summary>
        /// <param name="propertiesToRemove"></param>
        public void RemoveProperties(PropertyBag propertiesToRemove)
        {
            if (propertiesToRemove == null)
            {
                return;
            }

            foreach (Property p in propertiesToRemove.AllProperties)
            {
                RemoveProperty(p);
            }
        }

        /// <summary>
        /// Removes all properties in the given statbag
        /// </summary>
        /// <param name="propertiesToRemove"></param>
        public void RemoveProperties(Property[] propertiesToRemove)
        {
            if (propertiesToRemove == null)
            {
                return;
            }

            foreach (Property p in propertiesToRemove)
            {
                RemoveProperty(p);
            }
        }

        /// <summary>
        /// Removes a property object from the propertybag
        /// </summary>
        /// <param name="objStat">the stat to add</param>
        public void RemoveProperty(Property objProp)
        {
            bool wasRemoved = false;
            if (m_HashedProperties != null)
            {
                wasRemoved = m_HashedProperties.Remove(objProp.PropertyId);
            }
            else
            {
                // We must first find the property using its ID because the property object we have here might not be the physically same property object that is being passed in (it might just be the proxy).
                objProp = m_LinkedProperties.FirstOrDefault(p => p.PropertyId == objProp.PropertyId);
                if (objProp == null)
                {
                    return;
                }
                wasRemoved = m_LinkedProperties.Remove(objProp);
            }

            LinkedList<Property> map = null;
            map = m_PropertyKindMaps.FirstOrDefault(list => list.First.Value.PropertyType == objProp.PropertyType);
            if (map != null)
            {
                map.Remove(objProp);
            }

            if (map.Count == 0)
            {
                m_PropertyKindMaps.Remove(map);
            }
            
            if (wasRemoved)
            {
                NotifyPropertyRemoved(objProp);
            }

            objProp.Owner = null;
        }

        /// <summary>
        /// Removes a property object from the propertybag
        /// </summary>
        public void RemoveProperty(int id)
        {
            Property prop = GetProperty(id);
            if (prop != null)
            {
                RemoveProperty(prop);
            }
        }

        /// <summary>
        /// Removes a property object from the propertybag
        /// </summary>
        public void RemoveProperty(string id)
        {
            Property prop = GetProperty(id);
            if (prop != null)
            {
                RemoveProperty(prop);
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            IEnumerable col;
            if (m_HashedProperties != null)
            {
                col = m_HashedProperties.Values;
            }
            else
            {
                col = m_LinkedProperties;
            }

            int num = 0;
            foreach (Property p in col)
            {
                num++;
                sb.AppendLine(p.ToString());
            }
            sb.Insert(0, "------------------- Property bag [" + Name + "] with [" + num + " items]. -------------------\r\n");
            sb.AppendLine("------------------- End Property Bag [" + Name + "] -------------------");
            return sb.ToString();
        }

        /// <summary>
        /// Nukes all of the properties stored in the bag.
        /// </summary>
        public void ClearProperties()
        {
            if (m_HashedProperties != null)
            {
                m_HashedProperties.Clear();
            }
            else
            {
                m_LinkedProperties.Clear();
            }
        }
    }

  

}
