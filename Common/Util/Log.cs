using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.CompilerServices;


namespace Shared
{
    public delegate void LogMessageDelegate(string msg);
    public class Log
    {
        #region LogMessage Event
        private static LogMessageDelegate LogMessageInvoker;

        /// <summary>
        /// Fires when the central server connection has been severed, for any reason.
        /// </summary>
        public static event LogMessageDelegate LogMessage
        {
            add
            {
                AddHandler_LogMessage(value);
            }
            remove
            {
                RemoveHandler_LogMessage(value);
            }
        }

        
        private static void AddHandler_LogMessage(LogMessageDelegate value)
        {
            LogMessageInvoker = (LogMessageDelegate)Delegate.Combine(LogMessageInvoker, value);
        }

        
        private static void RemoveHandler_LogMessage(LogMessageDelegate value)
        {
            LogMessageInvoker = (LogMessageDelegate)Delegate.Remove(LogMessageInvoker, value);
        }

        private static void FireLogMessage(string msg)
        {
            if (LogMessageInvoker != null)
            {
                LogMessageInvoker(msg);
            }
        }
        #endregion


        public static void LogMsg(string msg)
        {
#if DEBUG
            //System.Diagnostics.Debug.WriteLine(msg);
#endif
            FireLogMessage(msg);
        }

    }
}
