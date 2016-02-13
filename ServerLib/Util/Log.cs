using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using log4net.Config;

namespace Shared
{
   
    public class Log1
    {
        static Log1()
        {
            //Load from App.Config file
            XmlConfigurator.Configure();            
        }

        public static ILog Logger(string name)
        {         
            return LogManager.GetLogger(name);
        }
       
    }
}
