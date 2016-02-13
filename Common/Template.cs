using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.XPath;
using System.IO;

namespace Shared
{
    /*
     <Item Name="Test Item # 1" GOT="GenericItem" Class="Shared.GenericItem">
      <Properties>
        <Property Name="Description" StringValue="Let's see if this works. Æegis says awesome."></Property>
        <Property Name="HP" LongValue="100"/>
        <Property Name="Skill" FloatValue="37.3"/>
        <Property Name="Speed" FloatValue="1.0"/>
      </Properties>
      <Stats>
        <Stat StatID="Gold" StartValue="5000"></Stat>
      </Stats>
    <Scripts>
        <Script Name="TestScript"/>
     </Scripts>
    </Item>
     */
    public class Template
    {
        public int StackCount { get; set; }
        public bool IsTransient { get; set; }
        public bool IsStatic { get; set; }
        public PropertyBag Properties { get; set; }
        public StatBag Stats { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public GOT GameObjectType { get; set; }
        private static Dictionary<string, Template> m_Templates;

        public static Dictionary<string, Template> Templates
        {
            get { return m_Templates; }
        }

        static Template()
        {
            m_Templates = new Dictionary<string, Template>();
        }

        public Template()
        {
            Properties = new PropertyBag();
            Stats = new StatBag();
            Scripts = new List<uint>();
            GameObjectType = GOT.None;
        }

        /// <summary>
        /// Object class name.  Full type name.
        /// </summary>
        public string Class { get; set; }

        public List<uint> Scripts
        {
            get;
            set;
        }

        public static bool LoadTemplate(XPathDocument docNav)
        {
            Template t = new Template();
            bool isValidTemplate = false;
            try
            {
                // Create a navigator to query with XPath.
                XPathNavigator nav = docNav.CreateNavigator();

                XPathNavigator root = nav.SelectSingleNode("//Item");

                t.Description = root.GetAttribute("Name", "");
                t.Class = root.GetAttribute("Class", "");

                bool isStatic = true;
                if (!bool.TryParse(root.GetAttribute("IsStatic", ""), out isStatic))
                {
                    isStatic = true; // default to static item
                }
                t.IsStatic = isStatic;

                bool isTransient = true;
                if (!bool.TryParse(root.GetAttribute("IsTransient", ""), out isTransient))
                {
                    isTransient = true; // default to Transient, non-persisting item
                }
                t.IsTransient = isTransient;

                int stackCount = 1;
                if (!int.TryParse(root.GetAttribute("StackCount", ""), out stackCount))
                {
                    stackCount = 1; // default to Transient, non-persisting item
                }
                t.StackCount = stackCount;

                if (t.Class.Length < 1)
                {
                    Log.LogMsg("Failed to load template [" + docNav.ToString() + "] - [Class (fully qualified type name) could not be determined. Make sure that the Class attribute is defined.]");
                    goto bailout;
                }

                GOT got = GOT.None;
                string gotStr = root.GetAttribute("GOT", "");
                try
                {
                    got = (GOT)Enum.Parse(typeof(GOT), gotStr);
                }
                catch { }

                if (got == GOT.None)
                {
                    Log.LogMsg("Failed to load template [" + docNav.ToString() + "] - [GOT (Game object type) could not be determined. Make sure that GOT value exists in GOT enum cs file.]");
                    goto bailout;
                }

                t.GameObjectType = got;

                string strExpression = "//Item/Properties/*";
                XPathNodeIterator NodeIter = nav.Select(strExpression);

                //Iterate through the results showing the element value.
                while (NodeIter.MoveNext())
                {
                    string name = NodeIter.Current.GetAttribute("Name", "");
                    if (name.Length < 1)
                    {
                        Log.LogMsg("Failed to load template [" + docNav.ToString() + "] - [Property Name attribute is missing.]");
                        goto bailout;
                    }

                    string stringValue = NodeIter.Current.GetAttribute("StringValue", "");
                    string intValue = NodeIter.Current.GetAttribute("IntValue", "");
                    string floatValue = NodeIter.Current.GetAttribute("FloatValue", "");
                    string longValue = NodeIter.Current.GetAttribute("LongValue", "");

                    if (stringValue.Length < 1 && intValue.Length < 1 && floatValue.Length < 1 && longValue.Length < 1)
                    {
                        Log.LogMsg("Failed to load template [" + docNav.ToString() + "] - [Must set either StringValue, IntValue, FloatValue or LongValue for property " + name + ".]");
                        goto bailout;
                    }

                    if (stringValue.Length > 0)
                    {
                        t.Properties.SetProperty(name, stringValue);
                    }
                    else if (floatValue.Length > 1)
                    {
                        try
                        {
                            float val = float.Parse(floatValue);
                            t.Properties.SetProperty(name, val);
                        }
                        catch
                        {
                            Log.LogMsg("Failed to load template [" + docNav.ToString() + "] - [Must specify a valid number for FloatValue for property " + name + ".]");
                            goto bailout;
                        }
                    }
                    else if (intValue.Length > 0)
                    {
                        try
                        {
                            int val = int.Parse(intValue);
                            t.Properties.SetProperty(name, val);
                        }
                        catch
                        {
                            Log.LogMsg("Failed to load template [" + docNav.ToString() + "] - [Must specify a valid number for IntValue for property " + name + ".]");
                            goto bailout;
                        }
                    }
                    else if (longValue.Length > 0)
                    {
                        try
                        {
                            long val = long.Parse(longValue);
                            t.Properties.SetProperty(name, val);
                        }
                        catch
                        {
                            Log.LogMsg("Failed to load template [" + docNav.ToString() + "] - [Must specify a valid number for LongValue for property " + name + ".]");
                            goto bailout;
                        }
                    }
                }

                strExpression = "//Item/Stats/*";
                NodeIter = nav.Select(strExpression);
                //Iterate through the results showing the element value.
                while (NodeIter.MoveNext())
                {
                    string id = NodeIter.Current.GetAttribute("StatID", "");

                    string value = NodeIter.Current.GetAttribute("StartValue", "");
                    if (value.Length < 1)
                    {
                        value = "0";
                    }

                    float val = float.Parse(value);
                    Stat prototype = StatManager.Instance.AllStats.GetStat(id);
                    if (prototype == null)
                    {
                        Log.LogMsg("Failed to load stat [" + id + "] for template [" + t.Description + "]. Make sure stat is defiend in Stats.xml.");
                        continue;
                    }

                    Stat ns = new Stat(
                        prototype.StatID,
                        prototype.DisplayName,
                        prototype.Description,
                        prototype.Group,
                        prototype.CurrentValue,
                        prototype.MinValue,
                        prototype.MaxValue);
                    ns.ForceValue(val);
                    t.Stats.AddStat(ns);
                }

                strExpression = "//Item/Scripts/*";
                NodeIter = nav.Select(strExpression);
                //Iterate through the results showing the element value.
                while (NodeIter.MoveNext())
                {
                    string name = NodeIter.Current.GetAttribute("Name", "");
                    if(!name.StartsWith("Scripts."))
                    {
                        name = "Scripts." + name;
                    }

                    if (name.Length > 0)
                    {
                        GameObjectScript s = GameObjectScript.GetScript(Factory.GetStableHash(name));
                        if (s == null)
                        {
                            Log.LogMsg("Unable to instantiate script [" + name + "] on template [" + docNav.ToString() + "]. Factory couldn't create script. Was it registered using Factor.Register()?");
                        }
                        else
                        {
                            t.Scripts.Add(Factory.GetStableHash(name));
                        }
                    }
                }

                isValidTemplate = true;
            }
            catch (Exception e)
            {
                isValidTemplate = false;
                Log.LogMsg("Failed to load template [" + docNav.ToString() + "] - [" + e.Message + "]");
            }

        bailout:
            if (isValidTemplate)
            {
                t.Name = t.Description.ToLower();// Path.GetFileNameWithoutExtension(template).ToLower();
                Templates.Add(t.Name, t);
                //Log1.Logger("Server").Info("Loaded template [" + Path.GetFileNameWithoutExtension(template) + "]");
            }
            else
            {
                Log.LogMsg("Failed to load template [" + docNav.ToString() + "]");
            }

            return isValidTemplate;
        }

        public static bool LoadTemplate(string template)
        {
            try
            {
                XPathDocument docNav = XMLHelper.LoadDocument(template, true);
                return LoadTemplate(docNav);
            }
            catch(Exception e)
            {
                return false;
            }
            return false;                       
        }

        public static int LoadTemplates()
        {
            try
            {               
                string[] templates = Directory.GetFiles(Environment.CurrentDirectory + "\\Config\\ItemTemplates\\", "*.xml");
                int numLoaded = 0;
                foreach (string template in templates)
                {
                    if(LoadTemplate(template))
                    {
                        numLoaded++;
                    }
                }
                return numLoaded;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Error loading Templates: " + e.Message);
            }

            return -1;
        }
    }
}
