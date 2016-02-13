using System;
using System.Collections.Generic;
using System.Text;

namespace Shared
{
    public enum PropertyNotificationType
    {
        PropertyChanged = 0,        
    }

    /// <summary>
    /// Encapsulates information about a Game/Match/Instance's properties being updates
    /// </summary>
    public class PacketGamePropertiesUpdateNotification : PacketGameMessage
    {
        public PacketGamePropertiesUpdateNotification()
            : base()
        {
            PacketSubTypeID = (int)LobbyGameMessageSubType.GamePropertiesUpdateNotification;
            TheGame = Guid.Empty;
            Kind = PropertyNotificationType.PropertyChanged;
            Properties = new Property[0];
            PropertyBagId = Guid.Empty;
        }

        /// <summary>
        /// The game in question
        /// </summary>
        public Guid TheGame { get; set; }

        /// <summary>
        /// What kind of change notification this is
        /// </summary>
        public PropertyNotificationType Kind { get; set; }

        /// <summary>
        /// The properties in question
        /// </summary>
        public Property[] Properties { get; set; }

        /// <summary>
        /// The property bag that the Properties belong to.
        /// </summary>
        public Guid PropertyBagId { get; set; }

        /// <summary>
        /// Should the properties listed be REMOVED from the indicated bag.
        /// </summary>
        public bool Remove { get; set; }

        public override bool DeSerialize(byte[] data, Pointer p)
        {
            base.DeSerialize(data, p);

            Remove = BitPacker.GetBool(data, p);
            PropertyBagId = new Guid(BitPacker.GetString(data, p));
            int numProps = BitPacker.GetInt(data, p);
            Properties = new Property[numProps];
            for (int i = 0; i < numProps; i++)
            {
                Properties[i] = BitPacker.GetProperty(data, p, null);
            }
            return true;
        }

        public override byte[] Serialize(Pointer p)
        {
            base.Serialize(p);
            BitPacker.AddBool(ref m_SerializeBuffer, p, Remove);
            BitPacker.AddString(ref m_SerializeBuffer, p, PropertyBagId.ToString());
            BitPacker.AddInt(ref m_SerializeBuffer, p, Properties.Length);
            for (int i = 0; i < Properties.Length; i++)
            {
                BitPacker.AddProperty(ref m_SerializeBuffer, p, Properties[i]);
            }
            return m_SerializeBuffer;
        }
       
    }
}
