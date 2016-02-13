using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using System.Reflection;
using System.IO;

namespace Shared
{
    public class XMLHelper
    {

        public delegate XPathDocument OnLoadXMLDocumentDelegate(string filepath, bool preserveWhiteSpace);

        #region OnLoadXMLDocument Event
        private static OnLoadXMLDocumentDelegate OnLoadXMLDocumentInvoker;

        /// <summary>
        /// Signal that the connection handshake is complete and a Rijndael key exchange occurred successfully.
        /// We may now mark packets as encrypted.
        /// </summary>
        public static event OnLoadXMLDocumentDelegate OnLoadXMLDocument
        {
            add
            {
                AddHandler_OnLoadXMLDocument(value);
            }
            remove
            {
                RemoveHandler_OnLoadXMLDocument(value);
            }
        }

        private static void AddHandler_OnLoadXMLDocument(OnLoadXMLDocumentDelegate value)
        {
            OnLoadXMLDocumentInvoker = (OnLoadXMLDocumentDelegate)Delegate.Combine(OnLoadXMLDocumentInvoker, value);
        }

        private static void RemoveHandler_OnLoadXMLDocument(OnLoadXMLDocumentDelegate value)
        {
            OnLoadXMLDocumentInvoker = (OnLoadXMLDocumentDelegate)Delegate.Remove(OnLoadXMLDocumentInvoker, value);
        }

        private static XPathDocument FireOnLoadXMLDocument(string filepath, bool preserveWhiteSpace)
        {
            if (OnLoadXMLDocumentInvoker != null)
            {
                return OnLoadXMLDocumentInvoker(filepath, preserveWhiteSpace);
            }

            return null;
        }
        #endregion

        /// <summary>
        /// Loads an XML document from disk and returns the appropriate node for query, or returns null on error
        /// </summary>
        /// <param name="filePath">fully qualified path to the file</param>
        /// <returns></returns>
        public static XPathDocument LoadDocument(string filePath, bool preserveWhiteSpace)
        {
            try
            {
                if (OnLoadXMLDocumentInvoker != null) return FireOnLoadXMLDocument(filePath, preserveWhiteSpace);

                //if (filePath.IndexOf("\\") == -1) // filename only?
                {
                    filePath = filePath.TrimStart(char.Parse("\\"));
                    // need to get assembly location, b/c Windows Services run in their own space
                    string directory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                    filePath = Path.Combine(directory, filePath);
                }

                if (!File.Exists(filePath))
                {
                    Log.LogMsg("Failed to load XML file " + filePath + ". It does not exist.");
                }
                XPathDocument doc = new XPathDocument(filePath, preserveWhiteSpace ? XmlSpace.Preserve : XmlSpace.None);
                return doc;
            }
            catch (Exception e)
            {
                Log.LogMsg("Failed to load XML file " + filePath + ". ");
            }

            return null;
        }

        /// <summary>
        /// Reads the character.xml file and returns a list of properties that that character should have
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static bool Character_GetPropertyTypesFromTemplate(string filePath, ref PropertyBag props, ref StatBag stats)
        {
            XPathDocument doc = LoadDocument(filePath, true);
            XPathNavigator nav = null;

            string[] sections = new string[] { "StringProperties", "IntProperties", "LongProperties", "FloatProperties" };
            try
            {
                if (doc == null)
                {
                    return false;
                }

                nav = doc.CreateNavigator();                
                for (int i = 0; i < sections.Length; i++)
                {
                    XPathNodeIterator iter = nav.Select(@"./Template/Character/PropertyBag/" + sections[i] + "/Property");
                    while (iter.MoveNext())
                    {
                        string id = iter.Current.GetAttribute("ID", "");                       
                        if (id == null || id.Length < 1)
                        {
                            Log.LogMsg("Error reading ID attribute in node " + iter.Current.InnerXml);
                            continue;
                        }

                        string name = iter.Current.GetAttribute("Name", "");
                        if (name == null)
                        {
                            name = "";
                        }

                        int propertyTypeID = int.Parse(id);

                        if (sections[i] == "IntProperties")
                        {
                            int value = int.Parse(iter.Current.Value);
                            props.SetProperty(name, propertyTypeID, value);
                        }
                        else if (sections[i] == "StringProperties")
                        {
                            props.SetProperty(name, propertyTypeID, iter.Current.Value);
                        }
                        else if (sections[i] == "FloatProperties")
                        {
                            float value = float.Parse(iter.Current.Value);
                            props.SetProperty(name, propertyTypeID, value);
                        }
                        else if (sections[i] == "LongProperties")
                        {
                            long value = long.Parse(iter.Current.Value);
                            props.SetProperty(name, propertyTypeID, value);
                        }
                    }
                }

                // Stats
                XPathNodeIterator statIter = nav.Select(@"./Template/Character/StatBag/Stat");
                while (statIter.MoveNext())
                {
                    int id = int.Parse(statIter.Current.GetAttribute("StatID", ""));

                    Stat proto = StatManager.Instance[id];
                    if (proto == null)
                    {
                        Log.LogMsg("Error reading character template. Stat id [" + id + "] was specified but was not loaded from the Stats.xml configuration file. Stat not added to character.");
                        continue;
                    }

                    float currentValue = float.Parse(statIter.Current.Value);
                    Stat s = new Stat(id, proto.DisplayName, proto.Description, proto.Group, currentValue,proto.MinValue, proto.MaxValue);
                    stats.AddStat(s);
                }
            }
            catch (Exception e)
            {
                Log.LogMsg("Failed to load character properties from template.");
                //Log.LogMsg("Exception thrown reading Character template. " + e.Message);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Reads the Stats.xml file and returns a list of Stat definitions that it contains
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static bool Stats_LoadDefinitions(string filePath, List<Stat> stats)
        {
            XPathDocument doc = LoadDocument(filePath, true);
            XPathNavigator nav = null;
            string curStat = "";
            try
            {
                if (doc == null)
                {
                    return false;
                }
                nav = doc.CreateNavigator();               
                XPathNodeIterator iter = nav.Select(@"//Stats/*");
                while (iter.MoveNext())
                {
                    string id = iter.Current.GetAttribute("StatID", "");
                    curStat = id;
                    if (id == null || id.Length < 1)
                    {
                        Log.LogMsg("Error reading ID attribute in node " + iter.Current.InnerXml);
                        continue;
                    }

                    string desc = iter.Current.GetAttribute("Desc", "");
                    if (desc == null)
                    {
                        desc = "";
                    }

                    string group = iter.Current.GetAttribute("Group", "");
                    if (group == null)
                    {
                        group = "";
                    }

                    string name = iter.Current.GetAttribute("Name", "");
                    if (name == null)
                    {
                        name = "";
                    }

                    string minValue = iter.Current.GetAttribute("MinValue", "");
                    if (minValue == null)
                    {
                        minValue = "";
                    }

                    string maxValue = iter.Current.GetAttribute("MaxValue", "");
                    if (maxValue == null)
                    {
                        maxValue = "";
                    }

                    string startValue = iter.Current.GetAttribute("StartValue", "");
                    if (startValue == null || startValue.Length < 1)
                    {
                        startValue = "0";
                    }

                    try
                    {
                        if (name.Length < 1)
                        {
                            throw new ArgumentException("Stat name can't be blank.");
                        }
                        
                        int propertyTypeID = int.Parse(id);
                        float fminValue = float.Parse(minValue);
                        float fmaxValue = float.Parse(maxValue);
                        float fstartValue = float.Parse(startValue);

                        Stat s = new Stat(propertyTypeID, name, desc, group, fstartValue, fminValue, fmaxValue);

                        stats.Add(s);
                    }
                    catch (Exception e)
                    {
                        Log.LogMsg("Error parsing stat data for [" + curStat + "]. [" + e.Message + "]");
                    }
                   
                }                                  
            }
            catch (Exception e)
            {
                Log.LogMsg("Exception thrown reading Stat definition. " + e.Message);
                return false;
            }

            return true;
        }

    }
}
