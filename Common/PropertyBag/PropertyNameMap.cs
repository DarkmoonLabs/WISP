using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared
{
    /// <summary>
    /// Maps property IDs to property name strings.  For instance.  Property #1 == "Name"
    /// </summary>
    public class PropertyNameMap
    {
        static PropertyNameMap()
        {
            m_MapIdToName = new Dictionary<int, string>();
            m_MapNameToId = new Dictionary<string, int>();
        }

        private static Dictionary<string, int> m_MapNameToId;
        private static Dictionary<string, int> MapNameToId
        {
            get { return m_MapNameToId; }
            set { m_MapNameToId = value; }
        }

        private static Dictionary<int, string> m_MapIdToName;
        private static Dictionary<int, string> MapIdToName
        {
            get { return m_MapIdToName; }
            set { m_MapIdToName = value; }
        }

        /// <summary>
        /// Connects a property type with a name that can be read by humans.  Returns false if the property ID is already registered.
        /// </summary>
        /// <returns>false if the property ID is already registered</returns>
        public static bool AddMapping(int propertyId, string propertyName)
        {
            if (MapNameToId.ContainsKey(propertyName))
            {
                // name already registered
                return false;
            }

            if (m_MapIdToName.ContainsKey(propertyId))
            {
                // id already registered
                return false;
            }

            m_MapIdToName.Add(propertyId, propertyName);
            m_MapNameToId.Add(propertyName, propertyId);
            return true;
        }

        public static int GetId(string name, int defaultId)
        {
            int id= 0;
            if (!MapNameToId.TryGetValue(name, out id))
            {
                return defaultId;
            }
            
            return id;
        }

        public static int GetId(string name)
        {
            return GetId(name, 0);
        }

        /// <summary>
        /// Gets the mapped name for a given property ID
        /// </summary>
        /// <returns>the name, or default value if not found</returns>
        public static string GetName(int propertyId, string defaultValue)
        {
            string name = "";
            m_MapIdToName.TryGetValue(propertyId, out name);
            if (name == null || name.Length == 0)
            {
                return defaultValue;
            }

            return name;
        }

        /// <summary>
        /// Gets the mapped name for a given property ID
        /// </summary>
        /// <returns>the name, or empty string if not found</returns>
        public static string GetName(int propertyId)
        {
            return GetName(propertyId, "");
        }

    }
}
