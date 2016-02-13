using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using Shared;

namespace LobbyBeholder
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            // Handle running the server service in standalone mode, i.e. outside of the
            // Windows Service sandbox.  Just start the process with the argument
            // "standalone" to run it as a regular application.  You can set this
            // argument in the "Project->Properties->Debug" options for easy debugging.

            if (args.Length == 0 || args[0].ToLower().Trim() != "standalone")
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[] { new WispService() };
                Log1.Logger("Server").Info("Starting server.");
                ServiceBase.Run(ServicesToRun);
            }
            else
            {
                // you will have to manually kill the process either through the
                // debugger or the task manager
                WispService service = new WispService();
                service.Setup();
                System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
            }
        }
    }
}
