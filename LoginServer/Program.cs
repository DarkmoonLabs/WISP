using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Threading;
using Microsoft.Win32;
using System.ServiceProcess;
using Zeus;

namespace Shared
{
    static class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 0 && args[0].ToLower().Trim() == "/i")
            {
                Log1.Logger("Server").Info("Installing server. Please wait...");
                WispServices.InstallService(Application.ExecutablePath, "WISPLogin", "Wisp Login Server");
            }
            else if (args.Length != 0 && args[0].ToLower().Trim() == "/u")
            {
                Log1.Logger("Server").Info("Uninstalling server. Please wait...");
                WispServices.UninstallService("WISPLogin");
            }
            else if (args.Length == 0)
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[] { new LoginServerProc() };
                Log1.Logger("Server").Info("Starting server. Please wait...");
                ServiceBase.Run(ServicesToRun);
            }
            else
            {
                // you will have to manually kill the process either through the
                // debugger or the task manager
                LoginServerProc service = new LoginServerProc();
                service.Setup();
                System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
            }
        }
    }
}
