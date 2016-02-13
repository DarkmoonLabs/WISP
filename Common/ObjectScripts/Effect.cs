using System.Collections.Generic;
using System;
using System.Xml;
using System.Xml.XPath;

namespace Shared
{
    public abstract class Effect : ISerializableWispObject
    {
        private static uint m_TypeHash = 0;
        public abstract uint TypeHash
        {
            get;            
        }


        /// <summary>
        /// Stores static script data which is read in from Effects.xml
        /// </summary>
        public static Dictionary<uint, EffectInfo> EffectInfos = new Dictionary<uint, EffectInfo>();

        public static Effect GetEffect(uint kind)
        {
            Effect e = null;
         
            e = Factory.Instance.CreateObject(kind) as Effect;
            if (e == null)
            {
                return null;
            }

            // Attach static script info
            EffectInfo ei = new EffectInfo();
            if (Effect.EffectInfos.TryGetValue(kind, out ei))
            {
                e.Information = ei;
            }

            return e;
        }

        public EffectInfo Information = new EffectInfo();

      
        public Effect()
        {
            LastTick = 0;
            EffectStart = 0;
        }

        /// <summary>
        /// When the buff was applied
        /// </summary>
        public long EffectStart { get; set; }

        /// <summary>
        /// The last time the effect ticked
        /// </summary>
        public long LastTick { get; set; }

        /// <summary>
        /// How much time the buff has left
        /// </summary>
        public long TimeRemaining { get; set; }

        /// <summary>
        /// The character that initiated this effect.  Mostly the "caster".
        /// </summary>
        public IGameObject Instigator { get; set; }

        public IGameObject Target { get; set; }

        /// <summary>
        /// Any arguments defined for this effect
        /// </summary>
        public Dictionary<string, object> Args { get; set; }

        public virtual string Tick()
        {
            return "";
        }

        public virtual string EffectExpired()
        {
            return "";
        }

        public static int ReadStaticEffectData()
        {
            try
            {
#if !SILVERLIGHT
                EffectInfos = new Dictionary<uint, EffectInfo>();
                XPathNavigator nav = null;
               
                XPathDocument docNav = null;
                XPathNodeIterator NodeIter = null;
                String strExpression = null;

                // Open the XML.
                docNav = XMLHelper.LoadDocument(Environment.CurrentDirectory + "\\Config\\Effects.xml", true);
                // Create a navigator to query with XPath.
                nav = docNav.CreateNavigator();
                strExpression = "//Effects/Effect";
                NodeIter = nav.Select(strExpression);

                int numLoaded = 0;

                //Iterate through the results showing the element value.
                while (NodeIter.MoveNext())
                {
                    EffectInfo ei = new EffectInfo();
                    ei.DurationKind = (EffectDurationType)Enum.Parse(typeof(EffectDurationType), NodeIter.Current.SelectSingleNode("DurationKind").Value);
                    ei.Duration = NodeIter.Current.SelectSingleNode("Duration").ValueAsLong;
                    ei.Group = NodeIter.Current.SelectSingleNode("Group").Value;
                    ei.DisplayName = NodeIter.Current.SelectSingleNode("DisplayName").Value;
                    ei.Description = NodeIter.Current.SelectSingleNode("Description").Value;
                    ei.TickLength = NodeIter.Current.SelectSingleNode("TickLength").ValueAsLong;

                    XPathNodeIterator ingIt = NodeIter.Current.Select("./EventsToListenTo/Event");
                    GameEventType[] listeners = new GameEventType[ingIt.Count];
                    int i = 0;
                    while (ingIt.MoveNext())
                    {
                        string strRes = ingIt.Current.GetAttribute("type", "");
                        GameEventType eType = (GameEventType)Enum.Parse(typeof(GameEventType), strRes);
                        listeners[i] = eType;
                        i++;
                    }
                    ei.EventsToListenTo = listeners;
                    ei.EffectKind = Factory.GetStableHash(ei.DisplayName);
                    Console.WriteLine("Loaded Effect data for " + ei.DisplayName);
                    if(EffectInfos.ContainsKey(ei.EffectKind))
                    {
                        Console.WriteLine("Skipping loading Effect due to Effect name collision for " + ei.DisplayName);
                        continue;
                    }
                    numLoaded++;
                    EffectInfos.Add(ei.EffectKind, ei);
                }

                return numLoaded;
#endif
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Error loading Effects: " + e.Message);
            }

            return -1;
        }



        public void Serialize(ref byte[] buffer, Pointer p)
        {
            BitPacker.AddLong(ref buffer, p, EffectStart);
            BitPacker.AddLong(ref buffer, p, LastTick);
            BitPacker.AddLong(ref buffer, p, TimeRemaining);
        }

        public void Deserialize(byte[] data, Pointer p)
        {
            EffectStart = BitPacker.GetLong(data, p);
            LastTick = BitPacker.GetLong(data, p);
            TimeRemaining = BitPacker.GetLong(data, p);
        }
    }
}
