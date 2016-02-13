using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Shared
{
    public class WispProperty : Property, IPropertyBagProperty<ISerializableWispObject>
    {
        public WispProperty(string propertyName, int propertyId, ISerializableWispObject val, PropertyBag owner)
            : base(propertyId, owner, propertyName)
        {
            PropertyType = (int)PropertyKind.WispObject;
            m_Value = val;
        }

        public override object PropertyValue()
        {
            return Value;
        }

        private ISerializableWispObject m_Value;
        public ISerializableWispObject Value
        {
            get { return m_Value; }
            set
            {
                if (m_Value != value)
                {
                    m_Value = value;
                    if (Owner != null) Owner.NotifyPropertyUpdated(this);
                }
            }
        }

        public override void SerializeValue(ref byte[] dat, Pointer p)
        {
            BitPacker.AddString(ref dat, p, Name);
            BitPacker.AddSerializableWispObject(ref dat, p, m_Value);
        }

        public override void DeserializeValue(byte[] dat, Pointer p)
        {
            Name = BitPacker.GetString(dat, p);
            m_Value = BitPacker.GetSerializableWispObject(dat, p);
        }

        public override string ToString()
        {
            return "WispProperty [ID " + PropertyId + "],  [Name " + Name + "] = " + Value.ToString();
        }
    }

    public class WispArrayProperty : Property, IPropertyBagProperty<ISerializableWispObject[]>
    {
        public WispArrayProperty(string propertyName, int propertyId, ISerializableWispObject[] val, PropertyBag owner)
            : base(propertyId, owner, propertyName)
        {
            PropertyType = (int)PropertyKind.WispArray;
            m_Value = val;
        }

        public override object PropertyValue()
        {
            return Value;
        }

        private ISerializableWispObject[] m_Value;
        public ISerializableWispObject[] Value
        {
            get
            {
                return m_Value;
            }
            set
            {
                if (m_Value != value)
                {
                    m_Value = value;
                    if (Owner != null) Owner.NotifyPropertyUpdated(this);
                }
            }
        }

        public override void SerializeValue(ref byte[] dat, Pointer p)
        {
            BitPacker.AddInt(ref dat, p, m_Value.Length);
            for (int i = 0; i < m_Value.Length; i++)
            {
                BitPacker.AddString(ref dat, p, Name);
                BitPacker.AddSerializableWispObject(ref dat, p, m_Value[i]);
            }
        }

        public override void DeserializeValue(byte[] dat, Pointer p)
        {
            int num = BitPacker.GetInt(dat, p);
            m_Value = new ISerializableWispObject[num];
            for (int i = 0; i < num; i++)
            {
                Name = BitPacker.GetString(dat, p);
                m_Value[i] = BitPacker.GetSerializableWispObject(dat, p);
            }
        }

        public override string ToString()
        {
            return "WispArrayProperty [ID " + PropertyId + "],  [Name " + Name + "], [Count " + Value.Length + "]";
        }
    }

    public class Int16Property : Property, IPropertyBagProperty<short>
    {
        public Int16Property(string propertyName, int propertyId, short val, PropertyBag owner) : base(propertyId, owner, propertyName)
        {
            PropertyType = (int)PropertyKind.Int16;
            m_Value = val;
        }

        public override object PropertyValue()
        {
            return Value;
        }
    
        private short m_Value;
        public short Value
        {
            get { return m_Value; }
            set
            {
                if (m_Value != value)
                {
                    m_Value = value;
                    if (Owner != null) Owner.NotifyPropertyUpdated(this);
                }
            }
        }

        public override void SerializeValue(ref byte[] dat, Pointer p)
        {
            BitPacker.AddString(ref dat, p, Name);
            BitPacker.AddShort(ref dat, p, m_Value);
        }

        public override void DeserializeValue(byte[] dat, Pointer p)
        {
            Name = BitPacker.GetString(dat, p);
            m_Value = BitPacker.GetShort(dat, p);
        }

        public override string ToString()
        {
            return "Int16Property [ID " + PropertyId + "],  [Name " + Name + "], [Value " + Value + "]";
        }
    }

    public class Int16ArrayProperty : Property, IPropertyBagProperty<short[]>
    {
        public Int16ArrayProperty(string propertyName, int propertyId, short[] val, PropertyBag owner)
            : base(propertyId, owner, propertyName)
        {
            PropertyType = (int)PropertyKind.Int16Array;
            m_Value = val;
        }

        public override object PropertyValue()
        {
            return Value;
        }
    
        private short[] m_Value;     
        public short[] Value
        {
            get
            {
                return m_Value;
            }
            set
            {
                if (m_Value != value)
                {
                    m_Value = value;
                    if (Owner != null) Owner.NotifyPropertyUpdated(this);
                }
            }
        }

        public override void SerializeValue(ref byte[] dat, Pointer p)
        {
            BitPacker.AddInt(ref dat, p, m_Value.Length);
            for (int i = 0; i < m_Value.Length; i++)
            {
                BitPacker.AddString(ref dat, p, Name);
                BitPacker.AddShort(ref dat, p, m_Value[i]);
            }
        }

        public override void DeserializeValue(byte[] dat, Pointer p)
        {
            int num = BitPacker.GetInt(dat, p);
            m_Value = new short[num];
            for (int i = 0; i < num; i++)
            {
                Name = BitPacker.GetString(dat, p);
                m_Value[i] = BitPacker.GetShort(dat, p);
            }
        }

        public override string ToString()
        {

            return "Int16ArrayProperty [ID " + PropertyId + "],  [Name " + Name + "], [Value " + String.Join(",", Value.Select(p => p.ToString()).ToArray()) + "]";
        }
    }

    public class GuidProperty : Property, IPropertyBagProperty<Guid>
    {
        public GuidProperty(string propertyName, int propertyId, Guid val, PropertyBag owner)
            : base(propertyId, owner, propertyName)
        {
            PropertyType = (int)PropertyKind.Guid;
            m_Value = val;
        }

        public override object PropertyValue()
        {
            return Value;
        }

        private Guid m_Value;
        public Guid Value
        {
            get { return m_Value; }
            set
            {
                if (m_Value != value)
                {
                    m_Value = value;
                    if (Owner != null) Owner.NotifyPropertyUpdated(this);
                }
            }
        }

        public override void SerializeValue(ref byte[] dat, Pointer p)
        {
            BitPacker.AddString(ref dat, p, Name);
            BitPacker.AddString(ref dat, p, m_Value.ToString());
        }

        public override void DeserializeValue(byte[] dat, Pointer p)
        {
            Name = BitPacker.GetString(dat, p);
            m_Value = new Guid(BitPacker.GetString(dat, p));
        }

        public override string ToString()
        {

            return "GuidProperty [ID " + PropertyId + "],  [Name " + Name + "], [Value " + Value.ToString() + "]";
        }
    }

    public class GuidArrayProperty : Property, IPropertyBagProperty<Guid[]>
    {
        public GuidArrayProperty(string propertyName, int propertyId, Guid[] val, PropertyBag owner)
            : base(propertyId, owner, propertyName)
        {
            PropertyType = (int)PropertyKind.GuidArray;
            m_Value = val;
        }

        public override object PropertyValue()
        {
            return Value;
        }

        private Guid[] m_Value;
        public Guid[] Value
        {
            get
            {
                return m_Value;
            }
            set
            {
                if (m_Value != value)
                {
                    m_Value = value;
                    if (Owner != null) Owner.NotifyPropertyUpdated(this);
                }
            }
        }

        public override void SerializeValue(ref byte[] dat, Pointer p)
        {
            BitPacker.AddInt(ref dat, p, m_Value.Length);
            for (int i = 0; i < m_Value.Length; i++)
            {
                BitPacker.AddString(ref dat, p, Name);
                BitPacker.AddString(ref dat, p, m_Value[i].ToString());
            }
        }

        public override void DeserializeValue(byte[] dat, Pointer p)
        {
            int num = BitPacker.GetInt(dat, p);
            m_Value = new Guid[num];
            for (int i = 0; i < num; i++)
            {
                Name = BitPacker.GetString(dat, p);
                m_Value[i] = new Guid(BitPacker.GetString(dat, p));
            }
        }

        public override string ToString()
        {
            return "Guid[] Property [ID " + PropertyId + "],  [Name " + Name + "], [Value " + String.Join(",", Value.Select(p => p.ToString()).ToArray()) + "]";
        }
    }

    public class Int32Property : Property, IPropertyBagProperty<int>
    {
        public Int32Property(string propertyName, int propertyId, int val, PropertyBag owner) : base(propertyId, owner, propertyName)
        {
            PropertyType = (int)PropertyKind.Int32;
            m_Value = val;
        }

        public override object PropertyValue()
        {
            return Value;
        }
    
        private int m_Value;
        public int Value
        {
            get { return m_Value; }
            set
            {
                if (m_Value != value)
                {
                    m_Value = value;
                    if (Owner != null) Owner.NotifyPropertyUpdated(this);
                }
            }
        }

        public override void SerializeValue(ref byte[] dat, Pointer p)
        {
            BitPacker.AddString(ref dat, p, Name);
            BitPacker.AddInt(ref dat, p, m_Value);
        }

        public override void DeserializeValue(byte[] dat, Pointer p)
        {
            Name = BitPacker.GetString(dat, p);
            m_Value = BitPacker.GetInt(dat, p);
        }

        public override string ToString()
        {

            return "Int32Property [ID " + PropertyId + "],  [Name " + Name + "], [Value " + Value.ToString() + "]";
        }
    }

    public class Int32ArrayProperty : Property, IPropertyBagProperty<int[]>
    {
         public Int32ArrayProperty(string propertyName, int propertyId, int[] val, PropertyBag owner)
            : base(propertyId, owner, propertyName)
        {
            PropertyType = (int)PropertyKind.Int32Array;
            m_Value = val;
        }

         public override object PropertyValue()
         {
             return Value;
         }
    
        private int[] m_Value;
        public int[] Value
        {
            get
            {
                return m_Value;
            }
            set
            {
                if (m_Value != value)
                {
                    m_Value = value;
                    if (Owner != null) Owner.NotifyPropertyUpdated(this);
                }
            }
        }

        public override void SerializeValue(ref byte[] dat, Pointer p)
        {
            BitPacker.AddInt(ref dat, p, m_Value.Length);
            for (int i = 0; i < m_Value.Length; i++)
            {
                BitPacker.AddString(ref dat, p, Name);
                BitPacker.AddInt(ref dat, p, m_Value[i]);
            }
        }

        public override void DeserializeValue(byte[] dat, Pointer p)
        {
            int num = BitPacker.GetInt(dat, p);
            m_Value = new int[num];
            for (int i = 0; i < num; i++)
            {
                Name = BitPacker.GetString(dat, p);
                m_Value[i] = BitPacker.GetInt(dat, p);
            }
        }

        public override string ToString()
        {

            return "Int32ArrayProperty [ID " + PropertyId + "],  [Name " + Name + "], [Value " + String.Join(",", Value.Select(p => p.ToString()).ToArray()) + "]";
        }
    }

    public class Int64Property : Property, IPropertyBagProperty<long>
    {
         public Int64Property(string propertyName, int propertyId, long val, PropertyBag owner) : base(propertyId, owner, propertyName)
        {
            PropertyType = (int)PropertyKind.Int64;
            m_Value = val;
        }

         public override object PropertyValue()
         {
             return Value;
         }
    
        private long m_Value;
        public long Value
        {
            get { return m_Value; }
            set
            {
                if (m_Value != value)
                {
                    m_Value = value;
                    if (Owner != null) Owner.NotifyPropertyUpdated(this);
                }
            }
        }

        public override void SerializeValue(ref byte[] dat, Pointer p)
        {
            BitPacker.AddString(ref dat, p, Name);
            BitPacker.AddLong(ref dat, p, m_Value);
        }

        public override void DeserializeValue(byte[] dat, Pointer p)
        {
            Name = BitPacker.GetString(dat, p);
            m_Value = BitPacker.GetLong(dat, p);
        }

        public override string ToString()
        {

            return "Int64Property [ID " + PropertyId + "],  [Name " + Name + "], [Value " + Value.ToString() + "]";
        }
    }

    public class Int64ArrayProperty : Property, IPropertyBagProperty<long[]>
    {
        public Int64ArrayProperty(string propertyName, int propertyId, long[] val, PropertyBag owner)
            : base(propertyId, owner, propertyName)
        {
            PropertyType = (int)PropertyKind.Int64Array;
            m_Value = val;
        }

        public override object PropertyValue()
        {
            return Value;
        }
    
        private long[] m_Value;
        public long[] Value
        {
            get
            {
                return m_Value;
            }
            set
            {
                if (m_Value != value)
                {
                    m_Value = value;
                    if (Owner != null) Owner.NotifyPropertyUpdated(this);
                }
            }
        }

        public override void SerializeValue(ref byte[] dat, Pointer p)
        {
            BitPacker.AddInt(ref dat, p, m_Value.Length);
            for (int i = 0; i < m_Value.Length; i++)
            {
                BitPacker.AddString(ref dat, p, Name);
                BitPacker.AddLong(ref dat, p, m_Value[i]);
            }
        }

        public override void DeserializeValue(byte[] dat, Pointer p)
        {
            int num = BitPacker.GetInt(dat, p);
            m_Value = new long[num];
            for (int i = 0; i < num; i++)
            {
                Name = BitPacker.GetString(dat, p);
                m_Value[i] = BitPacker.GetLong(dat, p);
            }
        }

        public override string ToString()
        {

            return "Int64ArrayProperty [ID " + PropertyId + "],  [Name " + Name + "], [Value " + String.Join(",", Value.Select(p => p.ToString()).ToArray()) + "]";
        }
    }

    public class StringProperty : Property, IPropertyBagProperty<string>
    {
        public StringProperty(string propertyName, int propertyId, string val, PropertyBag owner)
            : base(propertyId, owner, propertyName)
        {
            PropertyType = (int)PropertyKind.String;
            m_Value = val;
        }

        public override object PropertyValue()
        {
            return Value;
        }

        private string m_Value;
        public string Value
        {
            get { return m_Value; }
            set
            {
                if (m_Value != value)
                {
                    m_Value = value;
                    if (Owner != null) Owner.NotifyPropertyUpdated(this);
                }
            }
        }

        public override void SerializeValue(ref byte[] dat, Pointer p)
        {
            BitPacker.AddString(ref dat, p, Name);
            BitPacker.AddString(ref dat, p, m_Value);
        }

        public override void DeserializeValue(byte[] dat, Pointer p)
        {
            Name = BitPacker.GetString(dat, p);
            m_Value = BitPacker.GetString(dat, p);
        }

        public override string ToString()
        {

            return "StringProperty [ID " + PropertyId + "],  [Name " + Name + "], [Value " + Value+ "]";
        }
    }

    public class StringArrayProperty : Property, IPropertyBagProperty<string[]>
    {
        public StringArrayProperty(string propertyName, int propertyId, string[] val, PropertyBag owner)
            : base(propertyId, owner, propertyName)
        {
            PropertyType = (int)PropertyKind.StringArray;
            m_Value = val;
        }

        public override object PropertyValue()
        {
            return Value;
        }

        private string[] m_Value;
        public string[] Value
        {
            get
            {
                return m_Value;
            }
            set
            {
                if (m_Value != value)
                {
                    m_Value = value;
                    if (Owner != null) Owner.NotifyPropertyUpdated(this);
                }
            }
        }

        public override void SerializeValue(ref byte[] dat, Pointer p)
        {
            BitPacker.AddInt(ref dat, p, m_Value.Length);
            for (int i = 0; i < m_Value.Length; i++)
            {
                BitPacker.AddString(ref dat, p, Name);
                BitPacker.AddString(ref dat, p, m_Value[i]);
            }
        }

        public override void DeserializeValue(byte[] dat, Pointer p)
        {
            int num = BitPacker.GetInt(dat, p);
            m_Value = new string[num];
            for (int i = 0; i < num; i++)
            {
                Name = BitPacker.GetString(dat, p);
                m_Value[i] = BitPacker.GetString(dat, p);
            }
        }

        public override string ToString()
        {

            return "StringArrayProperty [ID " + PropertyId + "],  [Name " + Name + "], [Value " + String.Join(",", Value.Select(p => p).ToArray()) + "]";
        }
    }

    public class BoolProperty : Property, IPropertyBagProperty<bool>
    {
        public BoolProperty(string propertyName, int propertyId, bool val, PropertyBag owner)
            : base(propertyId, owner, propertyName)
        {
            PropertyType = (int)PropertyKind.Bool;
            m_Value = val;
        }

        public override object PropertyValue()
        {
            return Value;
        }

        private bool m_Value;
        public bool Value
        {
            get { return m_Value; }
            set
            {
                if (m_Value != value)
                {
                    m_Value = value;
                    if (Owner != null) Owner.NotifyPropertyUpdated(this);
                }
            }
        }

        public override void SerializeValue(ref byte[] dat, Pointer p)
        {
            BitPacker.AddString(ref dat, p, Name);
            BitPacker.AddBool(ref dat, p, m_Value);
        }

        public override void DeserializeValue(byte[] dat, Pointer p)
        {
            Name = BitPacker.GetString(dat, p);
            m_Value = BitPacker.GetBool(dat, p);
        }

        public override string ToString()
        {

            return "BoolProperty [ID " + PropertyId + "],  [Name " + Name + "], [Value " + Value.ToString() + "]";
        }
    }

    public class BoolArrayProperty : Property, IPropertyBagProperty<bool[]>
    {
        public BoolArrayProperty(string propertyName, int propertyId, bool[] val, PropertyBag owner)
            : base(propertyId, owner, propertyName)
        {
            PropertyType = (int)PropertyKind.BoolArray;
            m_Value = val;
        }

        public override object PropertyValue()
        {
            return Value;
        }

        private bool[] m_Value;
        public bool[] Value
        {
            get
            {
                return m_Value;
            }
            set
            {
                if (m_Value != value)
                {
                    m_Value = value;
                    if (Owner != null) Owner.NotifyPropertyUpdated(this);
                }
            }
        }

        public override void SerializeValue(ref byte[] dat, Pointer p)
        {
            BitPacker.AddInt(ref dat, p, m_Value.Length);
            for (int i = 0; i < m_Value.Length; i++)
            {
                BitPacker.AddString(ref dat, p, Name);
                BitPacker.AddBool(ref dat, p, m_Value[i]);
            }
        }

        public override void DeserializeValue(byte[] dat, Pointer p)
        {
            int num = BitPacker.GetInt(dat, p);
            m_Value = new bool[num];
            for (int i = 0; i < num; i++)
            {
                Name = BitPacker.GetString(dat, p);
                m_Value[i] = BitPacker.GetBool(dat, p);
            }
        }

        public override string ToString()
        {

            return "BoolArrayProperty [ID " + PropertyId + "],  [Name " + Name + "], [Value " + String.Join(",", Value.Select(p => p.ToString()).ToArray()) + "]";
        }
    }

    public class ByteProperty : Property, IPropertyBagProperty<byte>
    {
        public ByteProperty(string propertyName, int propertyId, byte val, PropertyBag owner)
            : base(propertyId, owner, propertyName)
        {
            PropertyType = (int)PropertyKind.Byte;
            m_Value = val;
        }

        public override object PropertyValue()
        {
            return Value;
        }

        private byte m_Value;
        public byte Value
        {
            get { return m_Value; }
            set
            {
                if (m_Value != value)
                {
                    m_Value = value;
                    if (Owner != null) Owner.NotifyPropertyUpdated(this);
                }
            }
        }

        public override void SerializeValue(ref byte[] dat, Pointer p)
        {
            BitPacker.AddString(ref dat, p, Name);
            BitPacker.AddByte(ref dat, p, m_Value);
        }

        public override void DeserializeValue(byte[] dat, Pointer p)
        {
            Name = BitPacker.GetString(dat, p);
            m_Value = BitPacker.GetByte(dat, p);
        }

        public override string ToString()
        {

            return "ByteProperty [ID " + PropertyId + "],  [Name " + Name + "], [Value " + Value.ToString() + "]";
        }
    }

    public class ByteArrayProperty : Property, IPropertyBagProperty<byte[]>
    {
        public ByteArrayProperty(string propertyName, int propertyId, byte[] val, PropertyBag owner)
            : base(propertyId, owner, propertyName)
        {
            PropertyType = (int)PropertyKind.ByteArray;
            m_Value = val;
        }

        public override object PropertyValue()
        {
            return Value;
        }

        private byte[] m_Value;
        public byte[] Value
        {
            get
            {
                return m_Value;
            }
            set
            {
                if (m_Value != value)
                {
                    m_Value = value;
                    if (Owner != null) Owner.NotifyPropertyUpdated(this);
                }
            }
        }

        public override void SerializeValue(ref byte[] dat, Pointer p)
        {
            BitPacker.AddString(ref dat, p, Name);
            BitPacker.AddBytes(ref dat, p, m_Value);
        }

        public override void DeserializeValue(byte[] dat, Pointer p)
        {
            Name = BitPacker.GetString(dat, p);
            m_Value = BitPacker.GetBytes(dat, p);
        }

        public override string ToString()
        {

            return "ByteArrayProperty [ID " + PropertyId + "],  [Name " + Name + "], [Value " + String.Join(",", Value.Select(p => p.ToString()).ToArray()) + "]";
        }
    }

    public class DateTimeProperty : Property, IPropertyBagProperty<DateTime>
    {
        public DateTimeProperty(string propertyName, int propertyId, DateTime val, PropertyBag owner)
            : base(propertyId, owner, propertyName)
        {
            PropertyType = (int)PropertyKind.DateTime;
            m_Value = val;
        }

        public override object PropertyValue()
        {
            return Value;
        }

        private DateTime m_Value;
        public DateTime Value
        {
            get { return m_Value; }
            set
            {
                if (m_Value != value)
                {
                    m_Value = value;
                    if (Owner != null) Owner.NotifyPropertyUpdated(this);
                }
            }
        }

        public override void SerializeValue(ref byte[] dat, Pointer p)
        {
            BitPacker.AddString(ref dat, p, Name);
            BitPacker.AddLong(ref dat, p, m_Value.Ticks);
        }

        public override void DeserializeValue(byte[] dat, Pointer p)
        {
            Name = BitPacker.GetString(dat, p);
            m_Value = new DateTime(BitPacker.GetLong(dat, p), DateTimeKind.Utc);
        }

        public override string ToString()
        {

            return "DateTimeProperty [ID " + PropertyId + "],  [Name " + Name + "], [Value " + Value.ToLongDateString() + " @ " + Value.ToLongTimeString() + "]";
        }
    }

    public class DateTimeArrayProperty : Property, IPropertyBagProperty<DateTime[]>
    {
        public DateTimeArrayProperty(string propertyName, int propertyId, DateTime[] val, PropertyBag owner)
            : base(propertyId, owner, propertyName)
        {
            PropertyType = (int)PropertyKind.DateTimeArray;
            m_Value = val;
        }

        public override object PropertyValue()
        {
            return Value;
        }

        private DateTime[] m_Value;
        public DateTime[] Value
        {
            get
            {
                return m_Value;
            }
            set
            {
                if (m_Value != value)
                {
                    m_Value = value;
                    if (Owner != null) Owner.NotifyPropertyUpdated(this);
                }
            }
        }

        public override void SerializeValue(ref byte[] dat, Pointer p)
        {
            BitPacker.AddInt(ref dat, p, m_Value.Length);
            for (int i = 0; i < m_Value.Length; i++)
            {
                BitPacker.AddString(ref dat, p, Name);
                BitPacker.AddLong(ref dat, p, m_Value[i].Ticks);
            }
        }

        public override void DeserializeValue(byte[] dat, Pointer p)
        {
            int num = BitPacker.GetInt(dat, p);
            m_Value = new DateTime[num];
            for (int i = 0; i < num; i++)
            {
                Name = BitPacker.GetString(dat, p);
                m_Value[i] = new DateTime(BitPacker.GetLong(dat, p), DateTimeKind.Utc);
            }
        }

        public override string ToString()
        {

            return "DateTimeArrayProperty [ID " + PropertyId + "],  [Name " + Name + "], [Value " + String.Join(",", Value.Select(p => p.ToLongDateString() + " @ " + p.ToLongTimeString()).ToArray()) + "]";
        }
    }

    public class SingleProperty : Property, IPropertyBagProperty<float>
    {
        public SingleProperty(string propertyName, int propertyId, float val, PropertyBag owner)
            : base(propertyId, owner, propertyName)
        {
            PropertyType = (int)PropertyKind.Single;
            m_Value = val;
        }

        public override object PropertyValue()
        {
            return Value;
        }

        private float m_Value;
        public float Value
        {
            get { return m_Value; }
            set
            {
                if (m_Value != value)
                {
                    m_Value = value;
                    if (Owner != null) Owner.NotifyPropertyUpdated(this);
                }
            }
        }

        public override void SerializeValue(ref byte[] dat, Pointer p)
        {
            BitPacker.AddString(ref dat, p, Name);
            BitPacker.AddSingle(ref dat, p, m_Value);
        }

        public override void DeserializeValue(byte[] dat, Pointer p)
        {
            Name = BitPacker.GetString(dat, p);
            m_Value = BitPacker.GetSingle(dat, p);
        }

        public override string ToString()
        {

            return "SingleProperty [ID " + PropertyId + "],  [Name " + Name + "], [Value " + Value.ToString() + "]";
        }
    }

    public class SingleArrayProperty : Property, IPropertyBagProperty<float[]>
    {
        public SingleArrayProperty(string propertyName, int propertyId, float[] val, PropertyBag owner)
            : base(propertyId, owner, propertyName)
        {
            PropertyType = (int)PropertyKind.SingleArray;
            m_Value = val;
        }

        public override object PropertyValue()
        {
            return Value;
        }

        private float[] m_Value;
        public float[] Value
        {
            get
            {
                return m_Value;
            }
            set
            {
                if (m_Value != value)
                {
                    m_Value = value;
                    if (Owner != null) Owner.NotifyPropertyUpdated(this);
                }
            }
        }

        public override void SerializeValue(ref byte[] dat, Pointer p)
        {
            BitPacker.AddInt(ref dat, p, m_Value.Length);
            for (int i = 0; i < m_Value.Length; i++)
            {
                BitPacker.AddString(ref dat, p, Name);
                BitPacker.AddSingle(ref dat, p, m_Value[i]);
            }
        }

        public override void DeserializeValue(byte[] dat, Pointer p)
        {
            int num = BitPacker.GetInt(dat, p);
            m_Value = new float[num];
            for (int i = 0; i < num; i++)
            {
                Name = BitPacker.GetString(dat, p);
                m_Value[i] = BitPacker.GetSingle(dat, p);
            }
        }

        public override string ToString()
        {

            return "SingleArrayProperty [ID " + PropertyId + "],  [Name " + Name + "], [Value " + String.Join(",", Value.Select(p => p.ToString()).ToArray()) + "]";
        }
    }

    public class DoubleProperty : Property, IPropertyBagProperty<double>
    {
        public DoubleProperty(string propertyName, int propertyId, double val, PropertyBag owner)
            : base(propertyId, owner, propertyName)
        {
            PropertyType = (int)PropertyKind.Double;
            m_Value = val;
        }

        public override object PropertyValue()
        {
            return Value;
        }

        private double m_Value;
        public double Value
        {
            get { return m_Value; }
            set
            {
                if (m_Value != value)
                {
                    m_Value = value;
                    if (Owner != null) Owner.NotifyPropertyUpdated(this);
                }
            }
        }

        public override void SerializeValue(ref byte[] dat, Pointer p)
        {
            BitPacker.AddString(ref dat, p, Name);
            BitPacker.AddDouble(ref dat, p, m_Value);
        }

        public override void DeserializeValue(byte[] dat, Pointer p)
        {
            Name = BitPacker.GetString(dat, p);
            m_Value = BitPacker.GetDouble(dat, p);
        }

        public override string ToString()
        {

            return "DoubleProperty [ID " + PropertyId + "],  [Name " + Name + "], [Value " + Value.ToString() + "]";
        }
    }

    public class DoubleArrayProperty : Property, IPropertyBagProperty<double[]>
    {
        public DoubleArrayProperty(string propertyName, int propertyId, double[] val, PropertyBag owner)
            : base(propertyId, owner, propertyName)
        {
            PropertyType = (int)PropertyKind.DoubleArray;
            m_Value = val;
        }

        public override object PropertyValue()
        {
            return Value;
        }

        private double[] m_Value;
        public double[] Value
        {
            get
            {
                return m_Value;
            }
            set
            {
                if (m_Value != value)
                {
                    m_Value = value;
                    if (Owner != null) Owner.NotifyPropertyUpdated(this);
                }
            }
        }

        public override void SerializeValue(ref byte[] dat, Pointer p)
        {
            BitPacker.AddInt(ref dat, p, m_Value.Length);
            for (int i = 0; i < m_Value.Length; i++)
            {
                BitPacker.AddString(ref dat, p, Name);
                BitPacker.AddDouble(ref dat, p, m_Value[i]);
            }
        }

        public override void DeserializeValue(byte[] dat, Pointer p)
        {
            int num = BitPacker.GetInt(dat, p);
            m_Value = new double[num];
            for (int i = 0; i < num; i++)
            {
                Name = BitPacker.GetString(dat, p);
                m_Value[i] = BitPacker.GetDouble(dat, p);
            }
        }

        public override string ToString()
        {

            return "DoubleArrayProperty [ID " + PropertyId + "],  [Name " + Name + "], [Value " + String.Join(",", Value.Select(p => p.ToString()).ToArray()) + "]";
        }
    }

    public class ComponentProperty : Property, IPropertyBagProperty<IComponent>
    {
        public ComponentProperty(string propertyName, int propertyId, IComponent val, PropertyBag owner)
            : base(propertyId, owner, propertyName)
        {
            PropertyType = (int)PropertyKind.Component;
            m_Value = val;
        }

        public override object PropertyValue()
        {
            return Value;
        }

        private IComponent m_Value;
        public IComponent Value
        {
            get { return m_Value; }
            set
            {
                if (m_Value != value)
                {
                    m_Value = value;
                    if (Owner != null) Owner.NotifyPropertyUpdated(this);
                }
            }
        }        

        public override void SerializeValue(ref byte[] dat, Pointer p)
        {
            if (m_Value == null)
            {
                return;
            }
            BitPacker.AddString(ref dat, p, Name);
            BitPacker.AddComponent(ref dat, p, m_Value);
        }

        public override void DeserializeValue(byte[] dat, Pointer p)
        {
            Name = BitPacker.GetString(dat, p);
            m_Value = BitPacker.GetComponent(dat, p);
        }

        public override string ToString()
        {

            return "ComponentProperty [ID " + PropertyId + "],  [Name " + Name + "], [Value " + Value.ComponentName + " : " + Value.ToString() + "]";
        }
    }

    public class ComponentArrayProperty : Property, IPropertyBagProperty<IComponent[]>
    {
        public ComponentArrayProperty(string propertyName, int propertyId, IComponent[] val, PropertyBag owner)
            : base(propertyId, owner, propertyName)
        {
            PropertyType = (int)PropertyKind.ComponentArray;
            m_Value = val;
        }

        public override object PropertyValue()
        {
            return Value;
        }

        private IComponent[] m_Value;
        public IComponent[] Value
        {
            get
            {
                return m_Value;
            }
            set
            {
                if (m_Value != value)
                {
                    m_Value = value;
                    if (Owner != null) Owner.NotifyPropertyUpdated(this);
                }
            }
        }

        public override void SerializeValue(ref byte[] dat, Pointer p)
        {
            BitPacker.AddInt(ref dat, p, m_Value.Length);
            for (int i = 0; i < m_Value.Length; i++)
            {
                BitPacker.AddString(ref dat, p, Name);
                BitPacker.AddComponent(ref dat, p, m_Value[i]);
            }
        }

        public override void DeserializeValue(byte[] dat, Pointer p)
        {
            int num = BitPacker.GetInt(dat, p);
            m_Value = new IComponent[num];
            for (int i = 0; i < num; i++)
            {
                Name = BitPacker.GetString(dat, p);
                m_Value[i] = BitPacker.GetComponent(dat, p);
            }
        }

        public override string ToString()
        {

            return "ComponentArrayProperty [ID " + PropertyId + "],  [Name " + Name + "], [Value " + String.Join(",", Value.Select(p => p.ComponentName + " : " +  p.ToString()).ToArray()) + "]";
        }

    }
}