using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using Shared;
using System.Reflection;
using Microsoft.Win32;
using System.Threading;

namespace PatchServer
{
    static class Program
    {

        private static bool InstallService(string exePath)
        {
            WispServiceTools.ServiceState state = WispServiceTools.ServiceInstaller.GetServiceStatus("Patchy");
            int maxTries = 20;
            int tries = 0;
            try
            {
                do
                {
                    switch (state)
                    {
                        case WispServiceTools.ServiceState.NotFound:
                        case WispServiceTools.ServiceState.Unknown:
                            Log1.Logger("Patcher").Info("No installed instance of Patchy service found.");
                            Log1.Logger("Patcher").Info("Installing Patchy service.");
                            WispServiceTools.ServiceInstaller.InstallAndStart("Patchy", "Patchy", exePath);
                            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\Patchy", "Description", "WISP Patch server.");

                            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Wisp\Servers\" + "Patchy", "ServiceName", "Patchy");
                            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Wisp\Servers\" + "Patchy", "Description", "WISP Patch server.");

                            break;
                        case WispServiceTools.ServiceState.Running:
                            Log1.Logger("Patcher").Info("Stopping Patchy service...");
                            WispServiceTools.ServiceInstaller.StopService("Patchy");
                            break;
                        case WispServiceTools.ServiceState.Stopped:
                            Log1.Logger("Patcher").Info("Patchy service is stopped.  Restarting...");
                            object val = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\Patchy", "ImagePath", "");
                            if (val == null)
                            {
                                WispServiceTools.ServiceInstaller.Uninstall("Patchy");
                                break;
                            }

                            Log1.Logger("Patcher").Info("Starting service...");
                            WispServiceTools.ServiceInstaller.InstallAndStart("Patchy", "Patchy", exePath);
                            //ServiceInstaller.StartService("Zeus");
                            break;
                        default:
                            Log1.Logger("Patcher").Info("Patchy service is " + state.ToString() + ". Waiting...");
                            break;
                    }

                    Thread.Sleep(500);
                    state = WispServiceTools.ServiceInstaller.GetServiceStatus("Patchy");
                    tries++;
                } while (tries < maxTries && state != WispServiceTools.ServiceState.Running);

                if (state != WispServiceTools.ServiceState.Running)
                {
                    Log1.Logger("Patcher").Info("Unable to start Patch service.  Check the Windows Event Log for Application errors.");
                    return false;
                }
            }
            catch (Exception e)
            {
                Log1.Logger("Patcher").Info("Error installing Patchy service. " + e.Message);
                return false;
            }

            return true;
        }

        static void Main(string[] args)
        {
            try
            {
                if (args.Length > 0)
                {
                    if (args[0] == "/i")
                    {
                        Log1.Logger("Patcher").Info("Installing PatchServer as service...");
                        InstallService(Assembly.GetExecutingAssembly().Location);                        
                    }
                    else if (args[0] == "/u")
                    {
                        Log1.Logger("Patcher").Info("UnInstalling PatchServer as service...");
                        WispServiceTools.ServiceInstaller.Uninstall("Patchy");
                    }
                    else if (args[0] == "standalone")
                    {
                        Log1.Logger("Patcher").Info("Running PatchServer in standalone...");
                        PatchServer MyService = new PatchServer();
                        MyService.Setup();
                        System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
                    }
                }
                else
                {
                    ServiceBase[] ServicesToRun;
                    ServicesToRun = new ServiceBase[] { new PatchServer() };
                    Log1.Logger("Patcher").Info("Starting Patch server as service.");
                    ServiceBase.Run(ServicesToRun);
                }
            }
            catch (Exception e)
            {
                Log1.Logger("Patcher").Info("Fatal error: " + e.Message);
            }
        }

        ///// <summary>
        ///// The main entry point for the application.
        ///// </summary>
        //static void Main(string[] args)
        //{
        //    if (args.Length == 0 || args[0].ToLower().Trim() != "standalone")
        //    {
        //        ServiceBase[] ServicesToRun;
        //        ServicesToRun = new ServiceBase[] { new PatchServer() };
        //        Log1.Logger("Patcher").Info("Starting server.");
        //        ServiceBase.Run(ServicesToRun);
        //    }
        //    else
        //    {
        //        // you will have to manually kill the process either through the
        //        // debugger or the task manager
        //        PatchServer service = new PatchServer();
        //        service.Setup();
        //        System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
        //    }
        //}
    }
}
