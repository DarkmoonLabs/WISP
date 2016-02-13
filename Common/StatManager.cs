using System;
using System.Net;
using System.Xml;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.IO;
using System.Xml.XPath;

namespace Shared
{
    public class StatManager
    {
        private static StatManager m_Instance = null;
        public static StatManager Instance
        {
            get
            {
                if (m_Instance == null)
                {
                    m_Instance = new StatManager();
                    m_Instance.AllStats = DeserializeFromXML();
                }
                return m_Instance;
            }
        }

        public StatManager()
        {
            AllStats = new StatBag();
        }

        public List<Stat> GetStatsInGroup(string group)
        {
            List<Stat> ret = new List<Stat>();
            foreach (Stat s in this.AllStats.AllStats)
            {
                if (s.Group == group)
                {
                    ret.Add(s);
                }
            }

            return ret;
        }

        public Stat this[int idx]
        {
            get
            {
                return AllStats.GetStat(idx);
            }
        }

        /// <summary>
        /// Where all our stats and buffs live
        /// </summary>
        public StatBag AllStats { get; set; }

        private static StatBag DeserializeFromXML()
        {
            StatBag bag = new StatBag();
            StatManager mgr = null;
            List<Stat> stats = new List<Stat>();
            XMLHelper.Stats_LoadDefinitions("\\Config\\Stats.xml", stats);
            foreach (Stat s in stats)
            {
                bag.AddStat(s);
            }

            return bag;
        }


    }
}
