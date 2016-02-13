using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace Shared
{
    /// <summary>
    /// Stores Server Performance Counter config sections.
    /// </summary>
    public class PerfConfig : ConfigurationSection
    {
        
        [ConfigurationProperty( "Counters" )]
        public PerfConfigCollection PerfItems
        {
            get { return ((PerfConfigCollection)(base["Counters"])); }
        }
    }

    [ConfigurationCollection(typeof(PerfConfigElement))]
    public class PerfConfigCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new PerfConfigElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((PerfConfigElement)(element)).GetHashCode();
        }

        public PerfConfigElement this[int idx]
        {
            get
            {
                return (PerfConfigElement)BaseGet(idx);
            }
        }
    }

    public class PerfConfigElement : ConfigurationElement
    {
        [ConfigurationProperty("Divisor", DefaultValue = "1", IsKey = false, IsRequired = false)]
        public string Divisor
        {
            get
            {
                return ((string)(base["Divisor"]));
            }
            set
            {
                base["Divisor"] = value;
            }
        }

        [ConfigurationProperty("PerformanceCounterType", DefaultValue = "", IsKey = false, IsRequired = false)]
        public string PerformanceCounterType
        {
            get
            {
                return ((string)(base["PerformanceCounterType"]));
            }
            set
            {
                base["PerformanceCounterType"] = value;
            }
        }

        [ConfigurationProperty("CounterName", DefaultValue = "", IsKey = false, IsRequired = true)]
        public string CounterName
        {
            get
            {
                return ((string)(base["CounterName"]));
            }
            set
            {
                base["CounterName"] = value;
            }
        }

        [ConfigurationProperty("CounterGroup", DefaultValue = "", IsKey = false, IsRequired = false)]
        public string CounterGroup
        {
            get
            {
                string blob = (string)(base["CounterGroup"]);               
                return blob;
            }
            set
            {
                base["CounterGroup"] = value;
            }
        }

        [ConfigurationProperty("InstanceName", DefaultValue = "", IsKey = false, IsRequired = true)]
        public string InstanceName
        {
            get
            {
                string blob = (string)base["InstanceName"];
                return blob;
            }
            set
            {
                base["InstanceName"] = value;
            }
        }

        [ConfigurationProperty("MaxSamplesInHistory", DefaultValue = "", IsKey = false, IsRequired = true)]
        public string MaxSamplesInHistory
        {
            get
            {
                return ((string)(base["MaxSamplesInHistory"]));
            }
            set
            {
                base["MaxSamplesInHistory"] = value;
            }
        }

        [ConfigurationProperty("SampleIntervalSecs", DefaultValue = "", IsKey = false, IsRequired = true)]
        public string SampleIntervalSecs
        {
            get
            {
                return ((string)(base["SampleIntervalSecs"]));
            }
            set
            {
                base["SampleIntervalSecs"] = value;
            }
        }

        [ConfigurationProperty("IsCustom", DefaultValue = "", IsKey = false, IsRequired = true)]
        public string IsCustom
        {
            get
            {
                return ((string)(base["IsCustom"]));
            }
            set
            {
                base["IsCustom"] = value;
            }
        }

        [ConfigurationProperty("Help", DefaultValue = "", IsKey = false, IsRequired = true)]
        public string Help
        {
            get
            {
                return ((string)(base["Help"]));
            }
            set
            {
                base["Help"] = value;
            }
        }


    }


}
