using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared
{
    /// <summary>
    /// Stores "handler" delegates based on "event type" and "event sub type" indexing.
    /// </summary>
    /// <typeparam name="arg1">delegate parameter 1 type</typeparam>
    /// <typeparam name="arg2">delegate parameter 2 type</typeparam>
    public class GenericHandlerMap<arg1, arg2>
    {
        private Dictionary<int, Action<arg1, arg2>> m_HandlerMap = new Dictionary<int,Action<arg1,arg2>>();
        private Dictionary<int, Dictionary<int, Action<arg1, arg2>>> m_HandlerSubtypeMaps = new Dictionary<int, Dictionary<int, Action<arg1, arg2>>>();

        /// <summary>
        /// Stores delegates to handlers, based on an integer "event type ID"
        /// </summary>
        private Dictionary<int, Action<arg1, arg2>> PrimaryHandlerMap
        {
            get { return m_HandlerMap; }
            set { m_HandlerMap = value; }
        }

        /// <summary>
        /// Stores delegates to handlers, based on the  message "event sub type ID", indexed by an "event type ID"
        /// </summary>
        private Dictionary<int, Dictionary<int, Action<arg1, arg2>>> SubtypeHandlerMap
        {
            get { return m_HandlerSubtypeMaps; }
            set { m_HandlerSubtypeMaps = value; }
        }

        /// <summary>
        /// Retrieves the multi-cast delegate for a particular event type ID
        /// </summary>
        public Action<arg1, arg2> GetHandlerDelegate(int eventTypeID)
        {
            return GetHandlerDelegate(eventTypeID, 0);
        }

        /// <summary>
        /// Retrieves the multi-cast delegate for a particular event type and event subtype ID
        /// </summary>
        public Action<arg1, arg2> GetHandlerDelegate(int eventTypeID, int eventSubTypeID)
        {
            Action<arg1, arg2> handler = null;
            Dictionary<int, Action<arg1, arg2>> map; // the map we look in for the handler
            int type = eventTypeID;
            
            if (eventSubTypeID == 0) // then this handler is found in the main map
            {
                map = m_HandlerMap;
            }
            else if (m_HandlerSubtypeMaps.TryGetValue(eventTypeID, out map)) //this handler sb found in the sub types map
            {
                type = eventSubTypeID;
            }
            else
            {
                return null;
            }

            if (map.TryGetValue(type, out handler))
            {
                return handler;
            }

            // last change... see if its in the main map, if we didnt just check the main map
            if (eventSubTypeID != 0 && m_HandlerMap.TryGetValue(eventTypeID, out handler))
            {
                return handler;
            }
            
            return null;
        }

        /// <summary>
        ///  Registers an action handler, based on a event type ID
        /// </summary>
        public void RegisterHandler(int eventTypeID, Action<arg1, arg2> handlerMethod)
        {
            RegisterHandler(eventTypeID, 0, handlerMethod);
        }

        /// <summary>
        ///  Registers an action handler, based on a event type ID and subtype ID
        /// </summary>
        public void RegisterHandler(int eventTypeID, int eventSubType, Action<arg1, arg2> handlerMethod)
        {
            Dictionary<int, Action<arg1, arg2>> map; // the map we store the handler in
            Action<arg1, arg2> handler = GetHandlerDelegate(eventTypeID, eventSubType);
            
            if (handler == null)
            {
                handler = handlerMethod;
                if (eventSubType == 0)
                {
                    lock (PrimaryHandlerMap)
                    {
                        PrimaryHandlerMap.Add(eventTypeID, handlerMethod);
                    }
                }
                else
                {
                    lock (SubtypeHandlerMap)
                    {
                        if (!SubtypeHandlerMap.TryGetValue(eventTypeID, out map))
                        {
                            map = new Dictionary<int, Action<arg1, arg2>>();
                            // map doesnt exist yet. add it.
                            SubtypeHandlerMap.Add(eventTypeID, map);
                        }
                        map.Add(eventSubType, handler);
                    }
                }
            }
            else // handler exists, append it
            {
                handler += handlerMethod;
                if (eventSubType == 0)
                {
                    lock (PrimaryHandlerMap)
                    {
                        PrimaryHandlerMap.Remove(eventTypeID);
                        PrimaryHandlerMap.Add(eventTypeID, handler);
                    }
                }
                else
                {
                    lock (SubtypeHandlerMap)
                    {
                        // this lookup shouldn't fail because if the handler exists (i.e. we're in this else block) then the dictionary exists
                        map = SubtypeHandlerMap[eventTypeID];
                        map.Remove(eventSubType);
                        map.Add(eventSubType, handler);
                    }
                }
            }
        }

        /// <summary>
        /// Removes a previously registered delegate from the handler map
        /// </summary>
        public void UnregisterHandler(int eventTypeID, Action<arg1, arg2> handlerMethod)
        {
            UnregisterHandler(eventTypeID, 0, handlerMethod);
        }

        /// <summary>
        /// Removes a previously registered delegate from the handler map
        /// </summary>
        public void UnregisterHandler(int eventTypeID, int eventSubTypeID, Action<arg1, arg2> handlerMethod)
        {
            Action<arg1, arg2> handler = GetHandlerDelegate(eventTypeID);
            if (handler == null)
            {
                return;
            }

            handler -= handlerMethod;
            
            if (eventSubTypeID == 0)
            {
                lock (PrimaryHandlerMap)
                {
                    PrimaryHandlerMap.Remove(eventTypeID);                    
                    if (handler.GetInvocationList().Length > 0)
                    {
                        PrimaryHandlerMap.Add(eventTypeID, handler);
                    }
                }
            }
            else
            {
                lock (SubtypeHandlerMap)
                {
                    Dictionary<int, Action<arg1, arg2>> map;
                    if (SubtypeHandlerMap.TryGetValue(eventTypeID, out map))
                    {
                        map.Remove(eventSubTypeID);
                        if (handler.GetInvocationList().Length > 0)
                        {
                            map.Add(eventSubTypeID, handler);
                        }
                        
                        if(map.Count < 1)
                        {
                            SubtypeHandlerMap.Remove(eventTypeID);   
                        }
                    }
                }
            }
        }


    }
}
