using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared
{
    public class LimitedMemoryLogAppender : log4net.Appender.MemoryAppender
    {
       
        public static int Count { get; set; }
        public int MaxLogEntriesToKeep { get; set; }
        private Queue<log4net.Core.LoggingEvent> m_Logs = new Queue<log4net.Core.LoggingEvent>();

        protected override void Append(log4net.Core.LoggingEvent loggingEvent)
        {
            m_Logs.Enqueue(loggingEvent);
            if (m_Logs.Count >= 25 && m_Logs.Count % 25 == 0)
            {
                Prune();
            }
        }

        private void Prune()
        {
            while (m_Logs.Count > MaxLogEntriesToKeep)
            {
                m_Logs.Dequeue();
            }
        }

        public override log4net.Core.LoggingEvent[] GetEvents()
        {
            Prune();
            return m_Logs.ToArray();
        }


    }
}
