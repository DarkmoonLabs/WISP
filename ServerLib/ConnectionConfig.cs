using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace Shared
{
    /// <summary>
    /// Stores Server Connection config sections.
    /// </summary>
    public class ConnectionConfigSection : ConfigurationSection
    {
        [ConfigurationProperty("ServerGroups")]
        public GroupCollection Groups
        {
            get { return ((GroupCollection)(base["ServerGroups"])); }
        }      

        [ConfigurationProperty("UpdateIntervalSecs", DefaultValue = "20", IsRequired = true)]
        [IntegerValidator()]
        public int UpdateIntervalSecs
        {
            get
            {
                return (int)this["UpdateIntervalSecs"];
            }
        }

    }

    [ConfigurationCollection(typeof(GroupElement), AddItemName = "Group", CollectionType = ConfigurationElementCollectionType.BasicMap)]
    public class GroupCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new GroupElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((GroupElement)(element)).ID;
        }

        public GroupElement this[int idx]
        {
            get
            {
                return (GroupElement)BaseGet(idx);
            }
        }
    }

    [ConfigurationCollection(typeof(ConnectionElement), AddItemName = "Server")]
    public class ConnectionCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new ConnectionElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ConnectionElement)(element)).ConnectionName;
        }

        public ConnectionElement this[int idx]
        {
            get
            {
                return (ConnectionElement)BaseGet(idx);
            }
        }
    }

    public class ConnectionElement : ConfigurationElement
    {
        [ConfigurationProperty("ConnectionName", DefaultValue = "", IsKey = true, IsRequired = true)]
        public string ConnectionName
        {
            get
            {
                return ((string)(base["ConnectionName"]));
            }
            set
            {
                base["ConnectionName"] = value;
            }
        }

        [ConfigurationProperty("Address", DefaultValue = "", IsKey = false, IsRequired = true)]
        public string Address
        {
            get
            {
                string blob = (string)(base["Address"]);               
                return blob;
            }
            set
            {
                base["Address"] = value;
            }
        }

        [ConfigurationProperty("Port", DefaultValue = "1", IsKey = false, IsRequired = true)]
        [IntegerValidator(ExcludeRange = false, MaxValue = 65535, MinValue = 1)]
        public int Port
        {
            get
            {
                return (int)base["Port"];
            }
            set
            {
                base["Port"] = value;
            }
        }

        [ConfigurationProperty("ServiceID", DefaultValue = "0", IsKey = false, IsRequired = true)]
        [IntegerValidator()]
        public int ServiceID
        {
            get
            {
                return (int)(base["ServiceID"]);
            }
            set
            {
                base["ServiceID"] = value;
            }
        }

        [ConfigurationProperty("SharedHiveKey", DefaultValue = "", IsKey = false, IsRequired = false)]
        public string SharedHiveKey
        {
            get
            {
                return ((string)(base["SharedHiveKey"]));
            }
            set
            {
                base["SharedHiveKey"] = value;
            }
        }
    }

    public class GroupElement : ConfigurationElement
    {
        [ConfigurationProperty("SharedHiveKey", DefaultValue = "00000000-0000-0000-0000-000000000000", IsRequired = true)]
        public string SharedHiveKey
        {
            get
            {
                return (string)this["SharedHiveKey"];
            }
        }

        [ConfigurationProperty("SessionDataConnectionString", DefaultValue = "", IsRequired = false)]
        public string SessionDataConnectionString
        {
            get
            {
                return (string)this["SessionDataConnectionString"];
            }
        }
        
        [ConfigurationProperty("ConnectMode", DefaultValue = "All", IsRequired = false)]
        public string ConnectMode
        {
            get
            {
                return (string)this["ConnectMode"];
            }
        }

        [ConfigurationProperty("ID", DefaultValue = "Default", IsKey=true, IsRequired = false)]
        public string ID
        {
            get
            {
                return (string)this["ID"];
            }
        }

        [ConfigurationProperty("Servers", IsDefaultCollection = true)]
        public ConnectionCollection ConnectionItems 
        { 
            get 
            { 
                return ((ConnectionCollection)(base["Servers"])); 
            } 
        }
      

    }

}
