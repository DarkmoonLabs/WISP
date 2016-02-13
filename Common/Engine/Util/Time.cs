using System;
using System.Collections.Generic;
using System.Text;

namespace Shared
{
    class Time
    {
        public static long GetGameTime()
        {
            return DateTime.Now.Ticks;
        }
    }
}
