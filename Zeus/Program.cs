using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using Shared;

namespace ZeusService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            if (args.Length == 0 || args[0].ToLower().Trim() != "standalone")
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[] 	{ new Zeus() };
                Log1.Logger("Server").Info("Starting server. Please wait...");
                ServiceBase.Run(ServicesToRun);
            }
            else
            {
                // you will have to manually kill the process either through the
                // debugger or the task manager
                Zeus service = new Zeus();
                service.Setup();
                System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
            }
        }



    }
}
