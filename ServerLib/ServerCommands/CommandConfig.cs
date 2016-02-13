using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace Shared
{
    /// <summary>
    /// Stores Server command config sections.
    /// </summary>
    public class CommandConfigSection : ConfigurationSection
    {
        
        [ConfigurationProperty( "Commands" )]
        public CommandCollection CommandItems
        {
            get { return ((CommandCollection)(base["Commands"])); }
        }
    }

    [ConfigurationCollection(typeof(CommandElement))]
    public class CommandCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new CommandElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((CommandElement)(element)).CommandName;
        }

        public CommandElement this[int idx]
        {
            get
            {
                return (CommandElement)BaseGet(idx);
            }
        }
    }

    public class CommandElement : ConfigurationElement
    {
        [ConfigurationProperty("CommandName", DefaultValue = "", IsKey = true, IsRequired = true)]
        public string CommandName
        {
            get
            {
                return ((string)(base["CommandName"]));
            }
            set
            {
                base["CommandName"] = value;
            }
        }

        [ConfigurationProperty("AllowedRoles", DefaultValue = "", IsKey = false, IsRequired = true)]
        public string AllowedRoles
        {
            get
            {
                string blob = (string)(base["AllowedRoles"]);               
                return blob;
            }
            set
            {
                base["AllowedRoles"] = value;
            }
        }

        [ConfigurationProperty("ParmNames", DefaultValue = "", IsKey = false, IsRequired = true)]
        public string ParmNames
        {
            get
            {
                string blob = (string)base["ParmNames"];
                return blob;
            }
            set
            {
                base["ParmNames"] = value;
            }
        }

        [ConfigurationProperty("Assembly", DefaultValue = "", IsKey = false, IsRequired = true)]
        public string Assembly
        {
            get
            {
                return ((string)(base["Assembly"]));
            }
            set
            {
                base["Assembly"] = value;
            }
        }

        [ConfigurationProperty("ClassName", DefaultValue = "", IsKey = false, IsRequired = true)]
        public string ClassName
        {
            get
            {
                return ((string)(base["ClassName"]));
            }
            set
            {
                base["ClassName"] = value;
            }
        }

        [ConfigurationProperty("MethodName", DefaultValue = "", IsKey = false, IsRequired = true)]
        public string MethodName
        {
            get
            {
                return ((string)(base["MethodName"]));
            }
            set
            {
                base["MethodName"] = value;
            }
        }      

        [ConfigurationProperty("UsageHelp", DefaultValue = "", IsKey = false, IsRequired = true)]
        public string UsageHelp
        {
            get
            {
                return ((string)(base["UsageHelp"]));
            }
            set
            {
                base["UsageHelp"] = value;
            }
        }

        [ConfigurationProperty("CommandGroup", DefaultValue = "", IsKey = false, IsRequired = true)]
        public string CommandGroup
        {
            get
            {
                return ((string)(base["CommandGroup"]));
            }
            set
            {
                base["CommandGroup"] = value;
            }
        }


    }


}
