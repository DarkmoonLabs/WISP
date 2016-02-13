using System;
using System.Collections.Generic;
using System.Text;
using Shared;

namespace Shared
{
    public enum MessageType
    {
        None
    }

    [Flags]
    public enum MessageClass
    {
        General = 2,
        GameSpecific = 4
    }

    /// <summary>
    /// A message that is sent from one game object to another
    /// </summary>
    public struct Telegram
    {
        public MessageClass MessageClass;
        public Guid Sender;
        public Guid Recipient;
        public MessageType MessageType;
        /// <summary>
        /// The time at which the message is to be sent - 0 to send it immediately
        /// </summary>
        public DateTime SendTime;


        public PropertyBag Parameters;


    }
}
