using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared
{
    /// <summary>
    /// Lots of static methods to help serialize common datatypes to a memory buffer.
    /// Be sure you "Get" the data back out in the same order that you "Add"ed it.
    /// This class will automatically adjust the size of the buffers as needed.
    /// </summary>
    public class BitPacker
    {
        public static void AddBytes(ref byte[] dat, Pointer curPointer, byte[] bytes)
        {
            AddInt(ref dat, curPointer, bytes.Length);
            EnsureBufferSize(ref dat, curPointer, bytes.Length);
            Util.Copy(bytes, 0, dat, curPointer.Advance(bytes.Length), bytes.Length);
        }

        /// <summary>
        /// Retr
        /// </summary>
        /// <param name="dat"></param>
        /// <param name="curPointer"></param>
        /// <returns></returns>
        public static byte[] GetBytes(byte[] dat, Pointer curPointer)
        {
            int len = GetInt(dat, curPointer);
            byte[] bytes = new byte[len];
            Util.Copy(dat, curPointer.Advance(len), bytes, 0, len);

            return bytes;
        }

        public static void AddStat(ref byte[] dat, Pointer curPointer, Stat stat)
        {
            AddStat(ref dat, curPointer, stat, true);
        }

        public static void AddStat(ref byte[] dat, Pointer curPointer, Stat stat, bool withInfoText)
        {
            AddInt(ref dat, curPointer, (int)stat.StatID);
            AddSingle(ref dat, curPointer, (float)stat.CurrentValue);
            AddSingle(ref dat, curPointer, (float)stat.MinValue);
            AddSingle(ref dat, curPointer, (float)stat.MaxValue);
            if (withInfoText)
            {
                AddString(ref dat, curPointer, stat.DisplayName);
                AddString(ref dat, curPointer, stat.Description);
                AddString(ref dat, curPointer, stat.Group);
            }
        }

        public static Stat GetStat(byte[] dat, Pointer p)
        {
            return GetStat(dat, p, true, null);
        }

        public static Stat GetStat(byte[] dat, Pointer p, bool withInfoText, StatBag bag)
        {
            Stat s = new Stat();
            s.StatID = GetInt(dat, p);
            float curVal = GetSingle(dat, p);
            s.MinValue = GetSingle(dat, p);
            s.MaxValue = GetSingle(dat, p);
            s.ForceValue(curVal);

            s.Owner = bag;
            if (withInfoText)
            {
                s.DisplayName = GetString(dat, p);
                s.Description = GetString(dat, p);
                s.Group = GetString(dat, p);
            }

            return s;
        }

        public static ISerializableWispObject GetSerializableWispObject(byte[] data, Pointer p)
        {
            uint type = GetUInt(data, p);
            ISerializableWispObject c = Factory.Instance.CreateObject(type) as ISerializableWispObject;
            c.Deserialize(data, p);
            return c;
        }
  
        public static void AddSerializableWispObject(ref byte[] data, Pointer curPointer, ISerializableWispObject c)
        {
            uint type = c.TypeHash;
            AddUInt(ref data, curPointer, type);
            c.Serialize(ref data, curPointer);
        }

        public static void AddComponent(ref byte[] data, Pointer curPointer, IComponent c, bool includeSubComponents)
        {
            uint type = c.TypeHash;
            AddUInt(ref data, curPointer, type);
            c.Serialize(ref data, curPointer, includeSubComponents);
        }

        public static void AddComponent(ref byte[] data, Pointer curPointer, IComponent c)
        {
            AddComponent(ref data, curPointer, c, true);
        }

        public static IComponent GetComponent(byte[] data, Pointer p)
        {
            return GetComponent(data, p, true);
        }

        public static IComponent GetComponent(byte[] data, Pointer p, bool includeSubComponents)
        {
            uint type = GetUInt(data, p);
            IComponent c = Factory.Instance.CreateObject(type) as IComponent;
            c.Deserialize(data, p, includeSubComponents);
            return c;
        }

        private static Dictionary<int, Func<string, int, PropertyBag, Property>> m_PropertyMap = new Dictionary<int, Func<string, int, PropertyBag, Property>>();

        public static Property CreateProperty(string propertyName, int propertyKind, int propertyDBID, PropertyBag owner)
        {
            Property prop = null;
            Func<string, int, PropertyBag, Property> factory = null;
            if (!m_PropertyMap.TryGetValue(propertyKind, out factory))
            {
                return null;
            }
            prop = factory(propertyName, propertyDBID, owner);
            return prop;
        }

        public static bool RegisterPropertyKindCreationDelegate(string propertyName, int propertyKind, Func<string, int, PropertyBag, Property> method)
        {
            if(m_PropertyMap.ContainsKey(propertyKind))
            {
                return false;
            }

            m_PropertyMap.Add(propertyKind, method);
            return true;
        }

        static BitPacker()
        {
            m_PropertyMap.Add((int)PropertyKind.Int32, (name, id, bag) => { return new Int32Property(name, id, 0, bag); });
            m_PropertyMap.Add((int)PropertyKind.String, (name, id, bag) => { return new StringProperty(name, id, "", bag); });
            m_PropertyMap.Add((int)PropertyKind.Bool, (name, id, bag) => { return new BoolProperty(name, id, false, bag); });
            m_PropertyMap.Add((int)PropertyKind.Guid, (name, id, bag) => { return new GuidProperty(name, id, Guid.Empty, bag); });
            m_PropertyMap.Add((int)PropertyKind.Single, (name, id, bag) => { return new SingleProperty(name, id, 0, bag); });
            m_PropertyMap.Add((int)PropertyKind.Int32Array, (name, id, bag) => { return new Int32ArrayProperty(name, id, new int[0], bag); });
            m_PropertyMap.Add((int)PropertyKind.StringArray, (name, id, bag) => { return new StringArrayProperty(name, id, new string[0], bag); });
            m_PropertyMap.Add((int)PropertyKind.DateTime, (name, id, bag) => { return new DateTimeProperty(name, id, DateTime.MinValue, bag); });
            m_PropertyMap.Add((int)PropertyKind.GuidArray, (name, id, bag) => { return new GuidArrayProperty(name, id, new Guid[0], bag); });
            m_PropertyMap.Add((int)PropertyKind.Double, (name, id, bag) => { return new DoubleProperty(name, id, 0, bag); });
            m_PropertyMap.Add((int)PropertyKind.Byte, (name, id, bag) => { return new ByteProperty(name, id, 0, bag); });
            m_PropertyMap.Add((int)PropertyKind.Component, (name, id, bag) => { return new ComponentProperty(name, id, null, bag); });
            m_PropertyMap.Add((int)PropertyKind.SingleArray, (name, id, bag) => { return new SingleArrayProperty(name, id, new float[0], bag); });
            m_PropertyMap.Add((int)PropertyKind.Int64, (name, id, bag) => { return new Int64Property(name, id, 0, bag); });
            m_PropertyMap.Add((int)PropertyKind.ComponentArray, (name, id, bag) => { return new ComponentArrayProperty(name, id, new Component[0], bag); });
            m_PropertyMap.Add((int)PropertyKind.DateTimeArray, (name, id, bag) => { return new DateTimeArrayProperty(name, id, new DateTime[0], bag); });
            m_PropertyMap.Add((int)PropertyKind.ByteArray, (name, id, bag) => { return new ByteArrayProperty(name, id, new byte[0], bag); });
            m_PropertyMap.Add((int)PropertyKind.DoubleArray, (name, id, bag) => { return new DoubleArrayProperty(name, id, new double[0], bag); });
            m_PropertyMap.Add((int)PropertyKind.Int16, (name, id, bag) => { return new Int16Property(name, id, 0, bag); });
            m_PropertyMap.Add((int)PropertyKind.Int64Array, (name, id, bag) => { return new Int64ArrayProperty(name, id, new long[0], bag); });
            m_PropertyMap.Add((int)PropertyKind.BoolArray, (name, id, bag) => { return new BoolArrayProperty(name, id, new bool[0], bag); });
            m_PropertyMap.Add((int)PropertyKind.WispObject, (name, id, bag) => { return new WispProperty(name, id, null, bag); });
            m_PropertyMap.Add((int)PropertyKind.WispArray, (name, id, bag) => { return new WispArrayProperty(name, id, new ISerializableWispObject[0], bag); });
        }

        public static Property GetProperty(byte[] data, Pointer p, PropertyBag bag)
        {
            int type = GetInt(data, p);
            int id = GetInt(data, p);
            string name = GetString(data, p);
            Property prop = CreateProperty(name, type, id, bag);
            if (prop == null)
            {
                return null;
            }
            prop.DeserializeValue(data, p);
            return prop;
        }

        public static PropertyBag GetPropertyBag(byte[] data, Pointer p)
        {
            PropertyBag bag = new PropertyBag();
            bag.Deserialize(data, p);
            return bag;
        }

        public static StatBag GetStatBag(byte[] data, Pointer p)
        {
            StatBag bag = new StatBag();
            bag.Name = GetString(data, p);
            bag.ID = new Guid(GetString(data, p));

            int num = GetInt(data, p);
            for (int i = 0; i < num; i++)
            {
                bag.AddStat(GetStat(data, p, true, bag));
            }

            return bag;
        }

        public static void AddProperty(ref byte[] dat, Pointer curPointer, Property p)
        {
            AddInt(ref dat, curPointer, (int)p.PropertyType);
            AddInt(ref dat, curPointer, p.PropertyId);
            AddString(ref dat, curPointer, p.Name);
            p.SerializeValue(ref dat, curPointer);
        }

        public static void AddPropertyBag(ref byte[] dat, Pointer curPointer, PropertyBag bag)
        {
            bag.Serialize(ref dat, curPointer);
        }

        public static void AddStatBag(ref byte[] dat, Pointer curPointer, StatBag bag)
        {
            Stat[] stats = bag.AllStats;
            AddString(ref dat, curPointer, bag.Name);
            AddString(ref dat, curPointer, bag.ID.ToString());
            AddInt(ref dat, curPointer, stats.Length);
            for (int i = 0; i < stats.Length; i++)
            {
                AddStat(ref dat, curPointer, stats[i]);
            }
        }

        public static List<string> GetStringList(byte[] dat, Pointer p)
        {
            List<string> stuff = new List<string>();
            int items = GetInt(dat, p);
            for (int i = 0; i < items; i++)
            {
                stuff.Add(GetString(dat, p));
            }

            return stuff;
        }

        public static void AddStringList(ref byte[] dat, Pointer curPointer, List<string> strings)
        {
            AddInt(ref dat, curPointer, strings.Count);
            for (int i = 0; i < strings.Count; i++)
            {
                AddString(ref dat, curPointer, strings[i]);
            }
        }

        public static List<int> GetIntList(ref byte[] dat, Pointer p)
        {
            List<int> stuff = new List<int>();
            int items = GetInt(dat, p);
            for (int i = 0; i < items; i++)
            {
                stuff.Add(GetInt(dat, p));
            }

            return stuff;
        }

        public static void AddIntList(ref byte[] dat, Pointer curPointer, List<int> ints)
        {
            AddInt(ref dat, curPointer, ints.Count);
            for (int i = 0; i < ints.Count; i++)
            {
                AddInt(ref dat, curPointer, ints[i]);
            }
        }

        public static List<float> GetSingleList(ref byte[] dat, Pointer p)
        {
            List<float> stuff = new List<float>();
            int items = GetInt(dat, p);
            for (int i = 0; i < items; i++)
            {
                stuff.Add(GetSingle(dat, p));
            }

            return stuff;
        }

        public static void AddSingleList(ref byte[] dat, Pointer curPointer, List<float> nums)
        {
            AddInt(ref dat, curPointer, nums.Count);
            for (int i = 0; i < nums.Count; i++)
            {
                AddSingle(ref dat, curPointer, nums[i]);
            }
        }

        public static SVector3 GetVector(byte[] dat, Pointer p)
        {
            SVector3 pos = new SVector3();
            pos.X = GetSingle(dat, p);
            pos.Y = GetSingle(dat, p);
            pos.Z = GetSingle(dat, p);

            return pos;
        }
        
        public static List<SVector3> GetVectorList(byte[] dat, Pointer p)
        {
            List<SVector3> stuff = new List<SVector3>();
            int items = GetInt(dat, p);
            if(items % 3 != 0)
            {
                return stuff;
            }

            for (int i = 0; i < items; i += 3)
            {
                float x = GetSingle(dat, p);
                float y = GetSingle(dat, p);
                float z = GetSingle(dat, p);

                stuff.Add(new SVector3(x, y, z));
            }

            return stuff;
        }

        public static void AddVectorList(ref byte[] dat, Pointer curPointer, List<SVector3> vecs)
        {
            AddInt(ref dat, curPointer, vecs.Count * 3);
            for (int i = 0; i < vecs.Count; i++)
            {
                AddSingle(ref dat, curPointer, (float)vecs[i].X);
                AddSingle(ref dat, curPointer, (float)vecs[i].Y);
                AddSingle(ref dat, curPointer, (float)vecs[i].Z);
            }
        }

        public static void AddVector(ref byte[] dat, Pointer curPointer, SVector3 vec)
        {
            AddSingle(ref dat, curPointer, (float)vec.X);
            AddSingle(ref dat, curPointer, (float)vec.Y);
            AddSingle(ref dat, curPointer, (float)vec.Z);
        }

        public static List<Guid> GetGuiList(byte[] dat, Pointer p)
        {
            List<Guid> stuff = new List<Guid>();
            int items = GetInt(dat, p);
            for (int i = 0; i < items; i++)
            {
                stuff.Add(new Guid(GetString(dat, p)));
            }

            return stuff;
        }

        public static void AddGuiList(ref byte[] dat, Pointer curPointer, List<Guid> ids)
        {
            AddInt(ref dat, curPointer, ids.Count);
            for (int i = 0; i < ids.Count; i++)
            {
                AddString(ref dat, curPointer, ids[i].ToString());
            }
        }

        #region Primitives

        private static void EnsureBufferSize(ref byte[] buffer, Pointer curPointer, int addedLen)
        {
            int left = buffer.Length - curPointer.Position;
            int newSize = buffer.Length;
            while (left < addedLen)
            {
                newSize *= 2;                
                left = newSize - curPointer.Position;
            }

            if (newSize > buffer.Length)
            {
                Array.Resize<byte>(ref buffer, newSize);
            }
            return;
        }

        public static void AddLong(ref byte[] dat, Pointer curPointer, long num)
        {
            byte[] bin = BitConverter.GetBytes(num);
            EnsureBufferSize(ref dat, curPointer, bin.Length);
            Util.Copy(bin, 0, dat, curPointer.Advance(bin.Length), bin.Length);
        }

        public static long GetLong(byte[] dat, Pointer p)
        {
            long num = BitConverter.ToInt64(dat, p.Advance(8));
            return num;
        }

        public static void AddByte(ref byte[] dat, Pointer p, byte bt)
        {
            EnsureBufferSize(ref dat, p, 1);
            dat[p.Advance(1)] = bt;
        }

        public static byte GetByte(byte[] dat, Pointer p)
        {
            return dat[p.Advance(1)];
        }

        public static void AddSingle(ref byte[] dat, Pointer curPointer, float num)
        {
            byte[] bin = BitConverter.GetBytes(num);
            EnsureBufferSize(ref dat, curPointer, bin.Length);
            Util.Copy(bin, 0, dat, curPointer.Advance(bin.Length), bin.Length);
        }

        public static float GetSingle(byte[] dat, Pointer p)
        {
            float num = BitConverter.ToSingle(dat, p.Advance(4));
            return num;
        }

        public static void AddDouble(ref byte[] dat, Pointer curPointer, double num)
        {
            byte[] bin = BitConverter.GetBytes(num);
            EnsureBufferSize(ref dat, curPointer, bin.Length);
            Util.Copy(bin, 0, dat, curPointer.Advance(bin.Length), bin.Length);
        }

        public static double GetDouble(byte[] dat, Pointer p)
        {
            double num = BitConverter.ToDouble(dat, p.Advance(8));
            return num;
        }

        public static void AddInt(ref byte[] dat, Pointer curPointer, int num)
        {
            byte[] bin = BitConverter.GetBytes(num);
            EnsureBufferSize(ref dat, curPointer, bin.Length);
            Util.Copy(bin, 0, dat, curPointer.Advance(bin.Length), bin.Length);
        }

        public static int GetInt(byte[] dat, Pointer p)
        {
            int num = BitConverter.ToInt32(dat, p.Advance(4));
            return num;
        }

        public static void AddUInt(ref byte[] dat, Pointer curPointer, uint num)
        {
            byte[] bin = BitConverter.GetBytes(num);
            EnsureBufferSize(ref dat, curPointer, bin.Length);
            Util.Copy(bin, 0, dat, curPointer.Advance(bin.Length), bin.Length);
        }

        public static uint GetUInt(byte[] dat, Pointer p)
        {
            uint num = BitConverter.ToUInt32(dat, p.Advance(4));
            return num;
        }

        public static void AddBool(ref byte[] dat, Pointer curPointer, bool val)
        {
            byte[] bin = BitConverter.GetBytes(val);
            EnsureBufferSize(ref dat, curPointer, bin.Length);
            Util.Copy(bin, 0, dat, curPointer.Advance(bin.Length), bin.Length);
        }

        public static bool GetBool(byte[] dat, Pointer p)
        {
            bool val = BitConverter.ToBoolean(dat, p.Advance(1));
            return val;
        }

        public static void AddShort(ref byte[] dat, Pointer curPointer, short num)
        {
            byte[] bin = BitConverter.GetBytes(num);
            EnsureBufferSize(ref dat, curPointer, bin.Length);
            Util.Copy(bin, 0, dat, curPointer.Advance(bin.Length), bin.Length);
        }

        public static short GetShort(byte[] dat, Pointer p)
        {
            short num = BitConverter.ToInt16(dat, p.Advance(2));
            return num;
        }

        public static void AddString(ref byte[] dat, Pointer curPointer, string words)
        {
            if (words == null) words = "";
            byte[] bin = System.Text.Encoding.UTF8.GetBytes(words);
            AddInt(ref dat, curPointer, bin.Length);

            EnsureBufferSize(ref dat, curPointer, bin.Length);
            Util.Copy(bin, 0, dat, curPointer.Advance(bin.Length), bin.Length);
        }

        public static string GetString(byte[] dat, Pointer p)
        {
            int wordsLen = BitConverter.ToInt32(dat, p.Advance(4));
            string words = System.Text.Encoding.UTF8.GetString(dat, p.Advance(wordsLen), wordsLen);
            return words;
        }


        #endregion

    }
}
