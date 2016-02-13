using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WispServiceTools;
using Microsoft.Win32;
using System.IO;
using System.Threading;
using Shared;

namespace Zeus
{
    public class WispServices
    {
        public static bool IsWispService(string name)
        {
            string[] services = GetInstalledServices();
            string[] parts;
            for (int i = 0; i < services.Length; i++)
            {
                parts = services[i].Split(char.Parse("|"));
                if (parts.Length != 3)
                {
                    Log1.Logger("Service").Error("GetInstalledServices array was malformed.  Wisp Registry entry may be corrupt => " + services[i]);
                    continue;
                }

                if (parts[0].ToLower() == name.ToLower())
                {
                    return true;
                }
            }

            return false;
        }

        public static string StartWispService(string name)
        {
            if (!IsWispService(name))
            {
                return name + " does not appear to be a known Wisp service. Can't start it.";
            }

            ServiceState state = ServiceInstaller.GetServiceStatus(name);
            int maxTries = 10;
            int tries = 0;
            string rs = "";
            try
            {
                do
                {
                    switch (state)
                    {
                        case ServiceState.NotFound:
                        case ServiceState.Unknown:
                            rs = name + " service isn't installed. Can't start it.";
                            break;
                        case ServiceState.Running:
                            rs = name + " service appears to already be running.";                            
                            break;
                        case ServiceState.Stopped:
                            rs = "Attempting to start " + name + " service.  Waiting...";
                            ServiceInstaller.StartService(name);
                            break;
                        default:
                            rs = "Attempting to start " + name + " service.  Waiting...";
                            break;
                    }

                    Thread.Sleep(500);
                    state = ServiceInstaller.GetServiceStatus(name);
                    tries++;
                } while (tries < maxTries && state != ServiceState.Running);
            }
            catch (Exception e)
            {
                rs = "Error starting " + name + " service. " + e.Message;
            }

            return rs;
        }

        public static string StopWispService(string name)
        {
            if (!IsWispService(name))
            {
                return name + " does not appear to be a known Wisp service. Can't stop it.";
            }

            ServiceState state = ServiceInstaller.GetServiceStatus(name);
            int maxTries = 10;
            int tries = 0;
            string rs = "";
            try
            {
                do
                {
                    switch (state)
                    {
                        case ServiceState.NotFound:
                        case ServiceState.Unknown:
                            rs = "Service isn't installed. Can't stop it.";
                            break;
                        case ServiceState.Running:
                            rs = "Attempting to stop service.  Waiting...";
                            ServiceInstaller.StopService(name);
                            break;
                        case ServiceState.Stopped:
                            rs = name + " is stopped.";
                            break;
                        default:
                            rs = "Attempting to stop service.  Waiting...";
                            break;
                    }

                    Thread.Sleep(500);
                    state = ServiceInstaller.GetServiceStatus(name);
                    tries++;
                } while (tries < maxTries && state != ServiceState.Stopped);
            }
            catch (Exception e)
            {
                rs = "Error stopping " + name + " service. " + e.Message;
            }

            return rs;
        }

        public static string[] GetInstalledServices()
        {
            string[] rsl = new string[0];
            RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wisp\Servers");
            if (key == null)
            {
                return rsl;
            }
            string[] servers = key.GetSubKeyNames();
            rsl = new string[servers.Length];
            for(int i = 0; i < servers.Length; i++)
            {
                try
                {
                    string server = servers[i];
                    string serverName = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Wisp\Servers\" + server, "ServiceName", "");
                    string desc = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Wisp\Servers\" + server, "Description", "");
                    ServiceState state = ServiceInstaller.GetServiceStatus(serverName);
                    rsl[i] = serverName + "|" + state.ToString() + "|" + desc;
                }
                catch (Exception e)
                {
                    Log1.Logger("Service").Error("Failed to get installed Wisp service info. " + e.Message);
                }
            }

            return rsl;
        }

        public static string UninstallService(string serviceName)
        {
            if (!IsWispService(serviceName))
            {
                return serviceName + " does not appear to be a known Wisp service.  Can't uninstall it.";
            }

            Log1.Logger("Service").Info("Checking for existing " + serviceName + " service.");
            ServiceState state = ServiceInstaller.GetServiceStatus(serviceName);
            int maxTries = 10;
            int tries = 0;
            bool uninstalled = false;
            try
            {
                do
                {
                    switch (state)
                    {
                        case ServiceState.NotFound:
                        case ServiceState.Unknown:
                            Registry.LocalMachine.DeleteSubKeyTree((@"SOFTWARE\Wisp\Servers\" + serviceName));
                            return serviceName + " is no longer installed on this machine.";
                        case ServiceState.Running:
                            Log1.Logger("Service").Info("Stopping " + serviceName + " service...");
                            ServiceInstaller.StopService(serviceName);
                            break;
                        case ServiceState.Stopped:
                            Log1.Logger("Service").Info(serviceName + " service is stopped.");
                            object val = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\" + serviceName, "ImagePath", "");
                            if (val != null)
                            {
                                uninstalled = ServiceInstaller.Uninstall(serviceName);
                                break;
                            }
                            break;
                        default:
                            Log1.Logger("Service").Info(serviceName + " service is " + state.ToString() + ". Waiting...");
                            break;
                    }
                    Thread.Sleep(500);
                    state = ServiceInstaller.GetServiceStatus(serviceName);
                    tries++;
                } while (!uninstalled && (tries < maxTries && state != ServiceState.Unknown && state != ServiceState.NotFound));

                if (state == ServiceState.Unknown || state == ServiceState.NotFound || uninstalled)
                {
                    Registry.LocalMachine.DeleteSubKeyTree((@"SOFTWARE\Wisp\Servers\" + serviceName));
                }

                string deployPath = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Wisp\", "TargetDir", Environment.CurrentDirectory);
                if (deployPath == null || deployPath.Length < 1)
                {
                    return "Uninstalled service, but unable to delete directory.  Deploy path is not set in the registry.";
                }

                string serverRoot = Path.Combine(deployPath, "Server", serviceName);
                if (Directory.Exists(serverRoot))
                {
                    Directory.Delete(serverRoot, true);
                }
            }
            catch (Exception e)
            {
                Log1.Logger("Service").Error("Error uninstalling " + serviceName + ". ", e);
                return "Error uninstalling " + serviceName + ". " + e.Message;
            }

            return serviceName + " service Uninstalled";
        }


        public static bool InstallService(string path, string serviceName, string description)
        {
            if (!File.Exists(path))
            {
                Log1.Logger("Service").Error("Error finding " + serviceName + " executable. After installation it should be in " + path + ", but it's not.");
                return false;
            }

            Log1.Logger("Service").Info("Checking for existing " + serviceName + " service.");
            ServiceState state = ServiceInstaller.GetServiceStatus(serviceName);
            int maxTries = 20;
            int tries = 0;
            try
            {
                do
                {
                    switch (state)
                    {
                        case ServiceState.NotFound:
                        case ServiceState.Unknown:
                            Log1.Logger("Service").Info("Now installed instance of " + serviceName + " service found.");
                            Log1.Logger("Service").Info("Installing " + serviceName + " service.");
                            ServiceInstaller.InstallAndStart(serviceName, serviceName, path);
                            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\" + serviceName, "Description", description);

                            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Wisp\Servers\" + serviceName, "ServiceName", serviceName);
                            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Wisp\Servers\" + serviceName, "Description", description);

                            break;
                        case ServiceState.Running:
                            Log1.Logger("Service").Info("Stopping " + serviceName + " service...");
                            ServiceInstaller.StopService(serviceName);
                            break;
                        case ServiceState.Stopped:
                            Log1.Logger("Service").Info(serviceName + " service is stopped. Restarting...");
                            object val = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\" + serviceName, "ImagePath", "");
                            if (val == null)
                            {
                                ServiceInstaller.Uninstall(serviceName);
                                break;
                            }
                            ServiceInstaller.InstallAndStart(serviceName, serviceName, path);

                            break;
                        default:
                            Log1.Logger("Service").Info(serviceName + " service is " + state.ToString() + ". Waiting...");
                            break;
                    }

                    Thread.Sleep(500);
                    state = ServiceInstaller.GetServiceStatus(serviceName);
                    tries++;
                } while (tries < maxTries && state != ServiceState.Running);

                if (state != ServiceState.Running)
                {
                    return false;   
                }
            }
            catch (Exception e)
            {
                Log1.Logger("Service").Error("Error installing " + serviceName + ". ", e);
                return false;
            }

            return true;
        }
    }
}
