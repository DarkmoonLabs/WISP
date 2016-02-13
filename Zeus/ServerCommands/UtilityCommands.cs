using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Security;

namespace Shared
{
    public class UtilityCommands
    {
        public string ConvertTicksToTimespan(string ticks, string executor)
        {
            string msg = "";

            try
            {
                long lticks = 0;
                if (!long.TryParse(ticks, out lticks))
                {
                    return "Must enter a valid 64-bit integer.";
                }

                TimeSpan ts = new TimeSpan(lticks);
                return string.Format("{0:%d} days {0:%h} hours {0:%m} minutes", ts);
            }
            catch (Exception e)
            {
                msg = "Error occurred trying to convert ticks to timespan user. " + e.Message;
            }

            return msg;
        }
    
    }
}
