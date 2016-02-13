using System;
using System.Collections.Generic;
using System.Text;

namespace Shared
{
    public delegate Packet PacketCreationDelegate();
    /// <summary>
    /// Creates objects derived from type @Packet based on a combination of typeID and subTypeID
    /// </summary> 
    /// <typeparam name="Packet"></typeparam>
    public class FactoryMap
    {
        private Dictionary<int, PacketCreationDelegate> m_HandlerMap = new Dictionary<int, PacketCreationDelegate>();
        private Dictionary<int, Dictionary<int, PacketCreationDelegate>> m_HandlerSubtypeMaps = new Dictionary<int, Dictionary<int, PacketCreationDelegate>>();

        /// <summary>
        /// Stores delegates to creation handlers, based on an integer "type ID"
        /// </summary>
        private Dictionary<int, PacketCreationDelegate> PrimaryHandlerMap
        {
            get { return m_HandlerMap; }
            set { m_HandlerMap = value; }
        }

        /// <summary>
        /// Stores delegates to creation handlers, based on the  message "sub type ID", indexed by an "type ID"
        /// </summary>
        private Dictionary<int, Dictionary<int, PacketCreationDelegate>> SubtypeHandlerMap
        {
            get { return m_HandlerSubtypeMaps; }
            set { m_HandlerSubtypeMaps = value; }
        }

        /// <summary>
        /// Retrieves the multi-cast delegate for a particular type ID
        /// </summary>
        public PacketCreationDelegate GetHandlerDelegate(int eventTypeID)
        {
            return GetHandlerDelegate(eventTypeID, 0);
        }

        /// <summary>
        /// Retrieves the creation delegate for a particular event type and subtype ID
        /// </summary>
        public PacketCreationDelegate GetHandlerDelegate(int typeID, int subTypeID)
        {
            PacketCreationDelegate handler = null;
            Dictionary<int, PacketCreationDelegate> map; // the map we look in for the handler
            int type = typeID;
            if (subTypeID == 0 || typeID == (int)PacketType.PacketGenericMessage) // then this handler is found in the main map
            {
                map = m_HandlerMap;
            }
            else if (m_HandlerSubtypeMaps.TryGetValue(typeID, out map)) //this handler sb found in the sub types map
            {
                type = subTypeID;
            }
            else
            {
                return null;
            }

            if (map.TryGetValue(type, out handler))
            {
                return handler;
            }

            return null;
        }

        /// <summary>
        ///  Registers an action handler, based on a type ID
        /// </summary>
        public bool RegisterHandler(int eventTypeID, PacketCreationDelegate handlerMethod)
        {
            return RegisterHandler(eventTypeID, 0, handlerMethod);
        }

        /// <summary>
        ///  Registers an action handler, based on a event type ID and subtype ID
        /// </summary>
        public bool RegisterHandler(int typeID, int subType, PacketCreationDelegate handlerMethod)
        {
            Dictionary<int, PacketCreationDelegate> map; // the map we store the handler in
            PacketCreationDelegate handler = GetHandlerDelegate(typeID, subType);

            if (handler == null)
            {
                handler = handlerMethod;
                if (subType == 0)
                {
                    lock (PrimaryHandlerMap)
                    {
                        PrimaryHandlerMap.Add(typeID, handlerMethod);
                    }
                }
                else
                {
                    lock (SubtypeHandlerMap)
                    {
                        if (!SubtypeHandlerMap.TryGetValue(typeID, out map))
                        {
                            map = new Dictionary<int, PacketCreationDelegate>();
                            // map doesnt exist yet. add it.
                            SubtypeHandlerMap.Add(typeID, map);
                        }
                        map.Add(subType, handler);
                    }
                }
                return true;
            }

            return false;
        }

        /// <summary>
        /// Removes a previously registered delegate from the handler map
        /// </summary>
        public void UnregisterHandler(int typeID, PacketCreationDelegate handlerMethod)
        {
            UnregisterHandler(typeID, 0, handlerMethod);
        }

        /// <summary>
        /// Removes a previously registered delegate from the handler map
        /// </summary>
        public void UnregisterHandler(int typeID, int subTypeID, PacketCreationDelegate handlerMethod)
        {
            PacketCreationDelegate handler = GetHandlerDelegate(typeID);
            if (handler == null)
            {
                return;
            }

            handler -= handlerMethod;

            if (subTypeID == 0)
            {
                lock (PrimaryHandlerMap)
                {
                    PrimaryHandlerMap.Remove(typeID);
                    if (handler.GetInvocationList().Length > 0)
                    {
                        PrimaryHandlerMap.Add(typeID, handler);
                    }
                }
            }
            else
            {
                lock (SubtypeHandlerMap)
                {
                    Dictionary<int, PacketCreationDelegate> map;
                    if (SubtypeHandlerMap.TryGetValue(typeID, out map))
                    {
                        map.Remove(subTypeID);
                        if (handler.GetInvocationList().Length > 0)
                        {
                            map.Add(subTypeID, handler);
                        }

                        if (map.Count < 1)
                        {
                            SubtypeHandlerMap.Remove(typeID);
                        }
                    }
                }
            }
        }


    }
}
