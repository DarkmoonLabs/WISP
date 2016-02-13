using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared
{ 
    /// <summary>
    /// Singleton factory that is used to register types that we want to transfer across the network.  When creating
    /// an ISerializableWispObject or subclassing a Component, you need to register the new class using this Factory,
    /// or Wisp will not know which object is coming across.  This registration needs to happen on the Client and the Server.
    /// </summary>
    public class Factory
    {
        private Factory()
        {
        }

        private static Factory m_Instance;
            
        /// <summary>
        /// Singleton instance accessor
        /// </summary>
        public static Factory Instance
        {
            get 
            {
                if (m_Instance == null)
                {
                    m_Instance = new Factory();
                }
                return m_Instance; 
            }
        }

        private Dictionary<uint, Func<object>> map = new Dictionary<uint, Func<object>>();

        /// <summary>
        /// Creates an instance of a previously registered type
        /// </summary>
        /// <param name="id">the ID under which the class was registered</param>
        /// <returns></returns>
        public object CreateObject(uint id)
        {
            Func<object> create = null;
            if (!map.TryGetValue(id, out create))
            {
                Log.LogMsg("Factory tried to instanstiate object with hash id [" + id + "], but that ID was not registered. Did you remember to call Factory.Register for that type?");
                return null;
            }

            return create();
        }

        /// <summary>
        /// Registers an object for creation.
        /// </summary>
        /// <param name="t">the type of the object being registered</param>
        /// <param name="factoryMethod">a method used to instantiate the object</param>
        /// <returns></returns>
        public bool Register(Type t, Func<object> factoryMethod)
        {
            uint id = GetStableHash(t.FullName);
            if (map.ContainsKey(id))
            {
                //log4net.LogManager.GetLogger("Server").Error("Type hash collision detected! Type [" + t.Name + "] has collided!");
                return false;
            }

            Log.LogMsg("Factory registering [" + t.ToString() + "] as id " + id);
            System.Console.WriteLine("Factory registering [" + t.ToString() + "] as id " + id);

            map.Add(id, factoryMethod);
            return true;
        }

        /// <summary>
        /// Unregisters a type from the Factory.  .
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public bool Unregister(Type t)
        {
            uint id = GetStableHash(t.FullName);
            return map.Remove(id);
        }

        /// <summary>
        /// Generates a stable integer hash given a type of an object.  It is theoretically possible
        /// for collisions to occur, however unlikely.  Watch the server startup log output to see if
        /// any collisions are detected.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static uint GetTypeHash(Type t)
        {
            uint id = GetStableHash(t.FullName);
            return id;
        }

        /// <summary>
        /// Generates a stable integer hash given a string.  It is theoretically possible
        /// for collisions to occur, however unlikely.  Watch the server startup log output to see if
        /// any collisions are detected.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static uint GetStableHash(string s)
        {
            uint hash = 0;
            // if you care this can be done much faster with unsafe  
            // using fixed char* reinterpreted as a byte* 
            foreach (byte b in System.Text.Encoding.Unicode.GetBytes(s))
            {
                hash += b;
                hash += (hash << 10);
                hash ^= (hash >> 6);
            }
            // final avalanche 
            hash += (hash << 3);
            hash ^= (hash >> 11);
            hash += (hash << 15);
            // helpfully we only want positive integer < MUST_BE_LESS_THAN 
            // so simple truncate cast is ok if not perfect 
            return hash;
        }

    }

}
