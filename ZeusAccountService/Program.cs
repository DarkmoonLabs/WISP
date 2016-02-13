using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using Shared;
using Microsoft.Win32;
using System.Threading;
using System.Reflection;

namespace ZeusAccountService
{
    static class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0 || args[0].ToLower().Trim() != "standalone")
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[] { new WispService() };
                Log1.Logger("Server").Info("Starting server. Please wait...");
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
