using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enyim.Caching.Memcached;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Shared;
using Enyim.Caching;
using Membase;

namespace ServerLib
{
    /// <summary>
    /// MemCached support class that allows storing and retrieving ISerializableWisoObject types to MemCached/MemBase
    /// Config explanation at https://github.com/enyim/EnyimMemcached/wiki/MembaseClient-Configuration
    /// </summary>
    public class WispMemCache : ITranscoder
    {
  	    private const ushort RawDataFlag = 0xfa52;
		private static readonly ArraySegment<byte> NullArray = new ArraySegment<byte>(new byte[0]);

        private static MembaseClient m_Client;
        /// <summary>
        /// Singleton MembaseClient accessor
        /// </summary>
        public static MembaseClient Client
        {
            get 
            {
                if (m_Client == null)
                {
                    m_Client = new MembaseClient();
                }
                return m_Client;
            }
        }
        
        //public static void Test()
        //{
        //    MemcachedClient c = new MemcachedClient();
            
        //    DateTime now = DateTime.Now;
        //    CommandData cd = new CommandData();
        //    Factory.Instance.Register(typeof(CommandData), delegate { return new CommandData(); });
        //    cd.AllowedRoles = new string[] { "Admin", "You" };
        //    cd.AssemblyName = "Some cool assembly";
        //    cd.ClassName = "Meh!";
        //    cd.CommandGroup = "Some group.";
        //    cd.CommandName = "TADA!";
        //    cd.ParmNames = new string[] { "Parm1", "Parm2" };

        //    bool result = c.Store(StoreMode.Set, "Testy1", cd, DateTime.MaxValue);
        //    for (int i = 0; i < 250000; i++)
        //    {
        //        object val = c.Get("Testy1");
        //    }

        //    DateTime end = DateTime.Now;
        //    TimeSpan len = end - now;
        //    string took = "Took " + len.TotalSeconds;
        //}

		public CacheItem Serialize(object value)
		{
			// raw data is a special case when some1 passes in a buffer (byte[] or ArraySegment<byte>)
			if (value is ArraySegment<byte>)
			{
				// ArraySegment<byte> is only passed in when a part of buffer is being 
				// serialized, usually from a MemoryStream (To avoid duplicating arrays 
				// the byte[] returned by MemoryStream.GetBuffer is placed into an ArraySegment.)
				return new CacheItem(RawDataFlag, (ArraySegment<byte>)value);
			}

			var tmpByteArray = value as byte[];

			// - or we just received a byte[]. No further processing is needed.
			if (tmpByteArray != null)
			{
				return new CacheItem(RawDataFlag, new ArraySegment<byte>(tmpByteArray));
			}

			ArraySegment<byte> data;
			TypeCode code = value == null ? TypeCode.DBNull : Type.GetTypeCode(value.GetType());

			switch (code)
			{
				case TypeCode.DBNull: data = this.SerializeNull(); break;
				case TypeCode.String: data = this.SerializeString((String)value); break;
				case TypeCode.Boolean: data = this.SerializeBoolean((Boolean)value); break;
				case TypeCode.Int16: data = this.SerializeInt16((Int16)value); break;
				case TypeCode.Int32: data = this.SerializeInt32((Int32)value); break;
				case TypeCode.Int64: data = this.SerializeInt64((Int64)value); break;
				case TypeCode.UInt16: data = this.SerializeUInt16((UInt16)value); break;
				case TypeCode.UInt32: data = this.SerializeUInt32((UInt32)value); break;
				case TypeCode.UInt64: data = this.SerializeUInt64((UInt64)value); break;
				case TypeCode.Char: data = this.SerializeChar((Char)value); break;
				case TypeCode.DateTime: data = this.SerializeDateTime((DateTime)value); break;
				case TypeCode.Double: data = this.SerializeDouble((Double)value); break;
				case TypeCode.Single: data = this.SerializeSingle((Single)value); break;
				default: data = this.SerializeObject(value); break;
			}

			return new CacheItem((ushort)((ushort)code | 0x0100), data);
		}

		public virtual object Deserialize(CacheItem item)
		{
			if (item.Data.Array == null)
				return null;

			if (item.Flags == RawDataFlag)
			{
				var tmp = item.Data;

				if (tmp.Count == tmp.Array.Length)
					return tmp.Array;

				// we should never arrive here, but it's better to be safe than sorry
				var retval = new byte[tmp.Count];

				Array.Copy(tmp.Array, tmp.Offset, retval, 0, tmp.Count);

				return retval;
			}

			var code = (TypeCode)(item.Flags & 0x00ff);

			var data = item.Data;

			switch (code)
			{
				// incrementing a non-existing key then getting it
				// returns as a string, but the flag will be 0
				// so treat all 0 flagged items as string
				// this may help inter-client data management as well
				//
				// however we store 'null' as Empty + an empty array, 
				// so this must special-cased for compatibilty with 
				// earlier versions. we introduced DBNull as null marker in emc2.6
				case TypeCode.Empty:
					return (data.Array == null || data.Count == 0)
							? null
							: DeserializeString(data);

				case TypeCode.DBNull: return null;
				case TypeCode.String: return this.DeserializeString(data);
				case TypeCode.Boolean: return this.DeserializeBoolean(data);
				case TypeCode.Int16: return this.DeserializeInt16(data);
				case TypeCode.Int32: return this.DeserializeInt32(data);
				case TypeCode.Int64: return this.DeserializeInt64(data);
				case TypeCode.UInt16: return this.DeserializeUInt16(data);
				case TypeCode.UInt32: return this.DeserializeUInt32(data);
				case TypeCode.UInt64: return this.DeserializeUInt64(data);
				case TypeCode.Char: return this.DeserializeChar(data);
				case TypeCode.DateTime: return this.DeserializeDateTime(data);
				case TypeCode.Double: return this.DeserializeDouble(data);
				case TypeCode.Single: return this.DeserializeSingle(data);
				case TypeCode.Object: return this.DeserializeObject(data);
				default: throw new InvalidOperationException("Unknown TypeCode was returned: " + code);
			}
		}

		#region [ Typed serialization ]

		protected virtual ArraySegment<byte> SerializeNull()
		{
			return NullArray;
		}

        protected virtual ArraySegment<byte> SerializeWispObject(ISerializableWispObject value)
        {
            byte[] buff = new byte[512];
            buff[0] = 255;
            buff[1] = 128;
            buff[2] = 255;

            Pointer p = new Pointer();
            p.Position = 3;
            BitPacker.AddSerializableWispObject(ref buff, p, value);
            return new ArraySegment<byte>(buff, 0, p.Position);
        }

		protected virtual ArraySegment<byte> SerializeString(string value)
		{
			return new ArraySegment<byte>(Encoding.UTF8.GetBytes((string)value));
		}

		protected virtual ArraySegment<byte> SerializeBoolean(bool value)
		{
			return new ArraySegment<byte>(BitConverter.GetBytes(value));
		}

		protected virtual ArraySegment<byte> SerializeInt16(Int16 value)
		{
			return new ArraySegment<byte>(BitConverter.GetBytes(value));
		}

		protected virtual ArraySegment<byte> SerializeInt32(Int32 value)
		{
			return new ArraySegment<byte>(BitConverter.GetBytes(value));
		}

		protected virtual ArraySegment<byte> SerializeInt64(Int64 value)
		{
			return new ArraySegment<byte>(BitConverter.GetBytes(value));
		}

		protected virtual ArraySegment<byte> SerializeUInt16(UInt16 value)
		{
			return new ArraySegment<byte>(BitConverter.GetBytes(value));
		}

		protected virtual ArraySegment<byte> SerializeUInt32(UInt32 value)
		{
			return new ArraySegment<byte>(BitConverter.GetBytes(value));
		}

		protected virtual ArraySegment<byte> SerializeUInt64(UInt64 value)
		{
			return new ArraySegment<byte>(BitConverter.GetBytes(value));
		}

		protected virtual ArraySegment<byte> SerializeChar(char value)
		{
			return new ArraySegment<byte>(BitConverter.GetBytes(value));
		}

		protected virtual ArraySegment<byte> SerializeDateTime(DateTime value)
		{
			return new ArraySegment<byte>(BitConverter.GetBytes(value.ToBinary()));
		}

		protected virtual ArraySegment<byte> SerializeDouble(Double value)
		{
			return new ArraySegment<byte>(BitConverter.GetBytes(value));
		}

		protected virtual ArraySegment<byte> SerializeSingle(Single value)
		{
			return new ArraySegment<byte>(BitConverter.GetBytes(value));
		}

		protected virtual ArraySegment<byte> SerializeObject(object value)
		{
            ISerializableWispObject w = value as ISerializableWispObject;
            if (w != null)
            {
                return SerializeWispObject(w);
            }
            
			using (var ms = new MemoryStream())
			{
				new BinaryFormatter().Serialize(ms, value);
				return new ArraySegment<byte>(ms.GetBuffer(), 0, (int)ms.Length);
			}
		}

		#endregion

		#region [ Typed deserialization ]

		protected virtual String DeserializeString(ArraySegment<byte> value)
		{
			return Encoding.UTF8.GetString(value.Array, value.Offset, value.Count);
		}

        protected virtual ISerializableWispObject DeserializeWispObject(ArraySegment<byte> value)
        {
            Pointer p = new Pointer();
            p.Position = value.Offset +3;
            return BitPacker.GetSerializableWispObject(value.Array, p);
        }

		protected virtual Boolean DeserializeBoolean(ArraySegment<byte> value)
		{
			return BitConverter.ToBoolean(value.Array, value.Offset);
		}

		protected virtual Int16 DeserializeInt16(ArraySegment<byte> value)
		{
			return BitConverter.ToInt16(value.Array, value.Offset);
		}

		protected virtual Int32 DeserializeInt32(ArraySegment<byte> value)
		{
			return BitConverter.ToInt32(value.Array, value.Offset);
		}

		protected virtual Int64 DeserializeInt64(ArraySegment<byte> value)
		{
			return BitConverter.ToInt64(value.Array, value.Offset);
		}

		protected virtual UInt16 DeserializeUInt16(ArraySegment<byte> value)
		{
			return BitConverter.ToUInt16(value.Array, value.Offset);
		}

		protected virtual UInt32 DeserializeUInt32(ArraySegment<byte> value)
		{
			return BitConverter.ToUInt32(value.Array, value.Offset);
		}

		protected virtual UInt64 DeserializeUInt64(ArraySegment<byte> value)
		{
			return BitConverter.ToUInt64(value.Array, value.Offset);
		}

		protected virtual Char DeserializeChar(ArraySegment<byte> value)
		{
			return BitConverter.ToChar(value.Array, value.Offset);
		}

		protected virtual DateTime DeserializeDateTime(ArraySegment<byte> value)
		{
			return DateTime.FromBinary(BitConverter.ToInt64(value.Array, value.Offset));
		}

		protected virtual Double DeserializeDouble(ArraySegment<byte> value)
		{
			return BitConverter.ToDouble(value.Array, value.Offset);
		}

		protected virtual Single DeserializeSingle(ArraySegment<byte> value)
		{
			return BitConverter.ToSingle(value.Array, value.Offset);
		}

		protected virtual object DeserializeObject(ArraySegment<byte> value)
		{
            try
            {
                ISerializableWispObject w = null;
                if (value.Count >= 3 && value.Array[value.Offset] == 255 && value.Array[value.Offset + 1] == 128 && value.Array[value.Offset + 2] == 255) // check for wisp hint
                {
                    w = DeserializeWispObject(value);
                    if (w == null)
                    {
                        throw new ArgumentException();
                    }
                    return w;
                }                
            }
            catch 
            {
            }

            using (var ms = new MemoryStream(value.Array, value.Offset, value.Count))
            {
                return new BinaryFormatter().Deserialize(ms);
            }
		}

		#endregion

    }
}
