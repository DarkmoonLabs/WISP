using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace Shared
{
    /// <summary>
    /// Stores any arbitrary data within an object.  Properties know how to send themselves
    /// across the wire
    /// </summary>
    public abstract class Property
    {        
        /// <summary>
        /// A value indicating what the data type of this property is (int, string, bool, etc).
        /// </summary>
        public int PropertyType { get; set; }

        private int m_PropertyId;
        /// <summary>
        /// The system ID of the property.  This is the number that's persisted in the DB.
        /// </summary>
        public int PropertyId
        {
            get { return m_PropertyId; }
            set { m_PropertyId = value; }
        }

        private bool m_IsLocalOnly;

        /// <summary>
        /// Local properties are not serialized
        /// </summary>
        public bool IsLocalOnly
        {
            get { return m_IsLocalOnly; }
            set { m_IsLocalOnly = value; }
        }

        public static Property Clone(Property target)
        {
            Property prop = null;
            int id = target.PropertyId;
            PropertyBag bag = target.Owner;
            
            switch (target.PropertyType)
            {
                case (int)PropertyKind.WispObject:
                    prop = new WispProperty(target.Name, id, ((WispProperty)target).Value, bag);
                    break;
                case (int)PropertyKind.WispArray:
                    prop = new WispArrayProperty(target.Name, id, ((WispArrayProperty)target).Value, bag);
                    break;
                case (int)PropertyKind.Int32:
                    prop = new Int32Property(target.Name, id, ((Int32Property)target).Value, bag);
                    break;
                case (int)PropertyKind.String:
                    prop = new StringProperty(target.Name, id, ((StringProperty)target).Value, bag);
                    break;
                case (int)PropertyKind.Bool:
                    prop = new BoolProperty(target.Name, id, ((BoolProperty)target).Value, bag);
                    break;
                case (int)PropertyKind.Guid:
                    prop = new GuidProperty(target.Name, id, ((GuidProperty)target).Value, bag);
                    break;
                case (int)PropertyKind.Single:
                    prop = new SingleProperty(target.Name, id, ((SingleProperty)target).Value, bag);
                    break;
                case (int)PropertyKind.Int32Array:
                    prop = new Int32ArrayProperty(target.Name, id, ((Int32ArrayProperty)target).Value, bag);
                    break;
                case (int)PropertyKind.StringArray:
                    prop = new StringArrayProperty(target.Name, id, ((StringArrayProperty)target).Value, bag);
                    break;
                case (int)PropertyKind.DateTime:
                    prop = new DateTimeProperty(target.Name, id, ((DateTimeProperty)target).Value, bag);
                    break;
                case (int)PropertyKind.GuidArray:
                    prop = new GuidArrayProperty(target.Name, id, ((GuidArrayProperty)target).Value, bag);
                    break;
                case (int)PropertyKind.Double:
                    prop = new DoubleProperty(target.Name, id, ((DoubleProperty)target).Value, bag);
                    break;
                case (int)PropertyKind.Byte:
                    prop = new ByteProperty(target.Name, id, ((ByteProperty)target).Value, bag);
                    break;
                case (int)PropertyKind.Component:
                    prop = new ComponentProperty(target.Name, id, ((ComponentProperty)target).Value, bag);
                    break;
                case (int)PropertyKind.SingleArray:
                    prop = new SingleArrayProperty(target.Name, id, ((SingleArrayProperty)target).Value, bag);
                    break;
                case (int)PropertyKind.Int64:
                    prop = new Int64Property(target.Name, id, ((Int64Property)target).Value, bag);
                    break;
                case (int)PropertyKind.ComponentArray:
                    prop = new ComponentArrayProperty(target.Name, id, ((ComponentArrayProperty)target).Value, bag);
                    break;
                case (int)PropertyKind.DateTimeArray:
                    prop = new DateTimeArrayProperty(target.Name, id, ((DateTimeArrayProperty)target).Value, bag);
                    break;
                case (int)PropertyKind.ByteArray:
                    prop = new ByteArrayProperty(target.Name, id, ((ByteArrayProperty)target).Value, bag);
                    break;
                case (int)PropertyKind.DoubleArray:
                    prop = new DoubleArrayProperty(target.Name, id, ((DoubleArrayProperty)target).Value, bag);
                    break;
                case (int)PropertyKind.Int16Array:
                    prop = new Int16ArrayProperty(target.Name, id, ((Int16ArrayProperty)target).Value, bag);
                    break;
                case (int)PropertyKind.Int16:
                    prop = new Int16Property(target.Name, id, ((Int16Property)target).Value, bag);
                    break;
                case (int)PropertyKind.Int64Array:
                    prop = new Int64ArrayProperty(target.Name, id, ((Int64ArrayProperty)target).Value, bag);
                    break;
                case (int)PropertyKind.BoolArray:
                    prop = new BoolArrayProperty(target.Name, id, ((BoolArrayProperty)target).Value, bag);
                    break;
            }
            prop.Name = target.Name;
            return prop;
        }        

        private string m_Name;							
        /// Field to hold the name of the property 
        /// <summary>
        /// The name of the Property
        /// </summary>
        public string Name
        {
            get
            {
                return m_Name;
            }
            set
            {
                if(value == m_Name)
                {
                    return;
                }
                if (Owner != null)
                {
                    Property p = Owner.GetProperty(value);
                    if (p != null)
                    {
                        Log.LogMsg("Can't set property Name for [" + m_PropertyId + "] on bag [" + Owner.Name + "]. The name [" + value + "] already exists in that bag.");
                        return;
                    }
                }
                m_Name = value;
            }
        }
        
        /// <summary>
        /// The property bag in which this property resides
        /// </summary>
        public PropertyBag Owner { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="PropertyId">The id by which this property is defined</param>
        /// <param name="Owner">The owner i.e. parent of the PropertyBag</param>
        public Property(int propertyId, PropertyBag owner) : this(propertyId, owner, "")
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="PropertyId">The id by which this property is defined</param>
        /// <param name="Owner">The owner i.e. parent of the PropertyBag</param>
        public Property(int propertyId, PropertyBag owner, string propertyName)
        {
            this.PropertyId = propertyId;
            Owner = owner;

            if (m_Name == null)
                m_Name = "";

            m_Name = propertyName;
        }

        /// <summary>
        /// Convert the property value to a byte array
        /// </summary>
        /// <returns></returns>
        public abstract void SerializeValue(ref byte[] dat, Pointer p);

        /// <summary>
        /// Convert a byte array into a property value.
        /// </summary>
        /// <param name="raw">a series of bytes that contains the property data</param>
        /// <param name="startIdx">the start index to begin reading</param>
        /// <returns>the startIndex + the number of consumed bytes</returns>
        public abstract void DeserializeValue(byte[] dat, Pointer p);

        public string StringValue
        {
            get
            {
                return PropertyValue().ToString();
            }
        }
        public abstract object PropertyValue();
    }
}
