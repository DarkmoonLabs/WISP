using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Web.Security;
using System.Web.Profile;
using ServerLib;
using System.IO;
using Zeus;
using Microsoft.Win32;

namespace Shared
{
	/// <summary>
	/// Zeus Server.  Handles Service Controller client connections and authentication of those users against the user database .
	/// </summary>
	public class ZeusServer : ServerBase
	{
        /// <summary>
        /// Handles receiving of new connections and creating individual socket connections for incoming login requests
        /// </summary>
        public ZeusServer()
            : base()
		{
            try
            {
                CharacterUtil.Instance.LoadCharacterTemplate();
                Factory.Instance.Register(typeof(WispConfigSettings), () => new WispConfigSettings());
                Factory.Instance.Register(typeof(WispUsersInfo), () => new WispUsersInfo());
                Factory.Instance.Register(typeof(WispUserDetail), () => new WispUserDetail());
                Factory.Instance.Register(typeof(WispCharacterDetail), () => new WispCharacterDetail(-1));
                Factory.Instance.Register(typeof(CommandData), delegate { return new CommandData(); });

                NetworkConnection.RegisterPacketCreationDelegate((int)PacketType.PacketStream, 1, delegate { return new PacketStream(); });

                //Roles.CreateRole("Administrator");
                //Membership.CreateUser("WispAdmin", "wisp123", "admin@home");
                //Roles.AddUserToRole("WispAdmin", "Administrator");

                AllowRemote = ConfigHelper.GetStringConfig("AllowRemoteConnections", "FALSE").ToUpper() == "TRUE";
                if (AllowRemote && PlayerAuthenticationType == AuthenticationType.None)
                {
                    Log1.Logger("Zeus.Server").Warn("PlayerAuthenticationType config was set to none. This is not allowed for Zeus.  Disallowing remote connections.");
                    AllowRemote = false;
                }


                //PerfMon.TrackSystemCounter("% Processor Time", "Processor", "_Total");
                //for (int i = 0; i < Environment.ProcessorCount; i++)
                //{
                //    PerfMon.TrackSystemCounter("% Processor Time", "Processor", i.ToString());
                //}

                //PerfMon.TrackSystemCounter("% Processor Time", "Process", PerfMon.ProcessName);
                //PerfMon.TrackSystemCounter("Thread Count", "Process", PerfMon.ProcessName);
                //PerfMon.TrackSystemCounter("Private Bytes", "Process", PerfMon.ProcessName);
                //PerfMon.TrackSystemCounter("# Bytes in all Heaps", ".NET CLR Memory", PerfMon.ProcessName);
                //PerfMon.TrackSystemCounter("Available MBytes", "Memory", "");
                //PerfMon.TrackSystemCounter("Contention Rate / sec", ".NET CLR LocksAndThreads", PerfMon.ProcessName);
                //PerfMon.TrackSystemCounter("Total # of Contentions", ".NET CLR LocksAndThreads", PerfMon.ProcessName);

                //PerfMon.AddCustomCounter("Packets Out", "Number of packets sent per second.", System.Diagnostics.PerformanceCounterType.RateOfCountsPerSecond32);
                //PerfMon.AddCustomCounter("Packets In", "Number of packets received per second.", System.Diagnostics.PerformanceCounterType.RateOfCountsPerSecond32);
                //PerfMon.AddCustomCounter("Live Connections", "Number of connected sockets.", System.Diagnostics.PerformanceCounterType.NumberOfItems32);
                //PerfMon.AddCustomCounter("Bandwidth Out", "Number of bytes sent per second.", System.Diagnostics.PerformanceCounterType.RateOfCountsPerSecond32);
                //PerfMon.AddCustomCounter("Bandwidth In", "Number of bytes received per second.", System.Diagnostics.PerformanceCounterType.RateOfCountsPerSecond32);

                //PerfMon.InstallCustomCounters();

                //PerfMon.StartSamplingCounters();
            }
            catch (Exception e)
            {
                Log1.Logger("Server").Fatal("Zeus failed to initialize. ", e);
            }
		}

        public override void StartServer()
        {
            base.StartServer();
            // FORMAT: serverNAME|state|desc
            string[] services = WispServices.GetInstalledServices();
            for (int i = 0; i < services.Length; i++)
            {
                string[] parts = services[i].Split(char.Parse("|"));
                if (parts.Length < 3)
                {
                    continue;
                }

                string name = parts[0];
                string state = parts[1];
                string desc = parts[2];

                object val = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\" + name, "ImagePath", "");
                if (val == null)
                {
                    Log1.Logger("Server").Error("Wisp registry claims that service [" + name + "] is installed, but the Windows registry does not agree.");
                    continue;
                }

                string exePath = val.ToString();
                string directory = Path.GetDirectoryName(exePath);
                if (!Directory.Exists(directory))
                {
                    Log1.Logger("Server").Error("Windows registry claims that Wisp service [" + name + "] is installed at [" + directory + "] but that directory was not found.");
                    continue;
                }

                string targetConfig = exePath + ".config";
                if (!File.Exists(targetConfig))
                {
                    Log1.Logger("Server").Error("Unable to locate config file [" + targetConfig + "] for service [" + name + "] and so the connection port can't be determnined.");
                    continue;
                }

                int port = 0;
                try
                {
                    string configContents = File.ReadAllText(targetConfig);
                    int loc = configContents.IndexOf("ListenOnPort");
                    if (loc < 0)
                    {
                        Log1.Logger("Server").Error("Failed to find 'ListenOnPort' directive in config file [" + targetConfig + "] for service [" + name + "]. Unable to determine port for that service.");
                        continue;
                    }

                    int valLoc = configContents.IndexOf("\"", loc+13);
                    int valEndLoc = configContents.IndexOf("\"", valLoc+1);
                    string sport = configContents.Substring(valLoc+1, valEndLoc-valLoc-1);
                    if (!int.TryParse(sport, out port) || port <= 0)
                    {
                        Log1.Logger("Server").Error("'ListenOnPort' directive in config file [" + targetConfig + "] for service [" + name + "] was in the wrong format - [" + sport +"]. Unable to determine port for that service.");
                        continue;
                    }
                }
                catch (Exception e)
                {
                    Log1.Logger("Server").Error("Failed to read config file [" + targetConfig + "] for service [" + name + "]. Unable to determine port for that service.", e);
                    continue;
                }

                GameServerInfo<OutboundServerConnection> gsi = new GameServerInfo<OutboundServerConnection>();
                gsi.HostName = "localhost";
                gsi.Name = name;
                string ip = "localhost";
                try
                {
                    gsi.ServiceID = 7; // 7eus
                    IPHostEntry iphe = Dns.GetHostEntry("localhost"); // this call will delay the server from starting if it doesn't resolve fast enough.

                    bool gotOne = false;
                    foreach (IPAddress addy in iphe.AddressList)
                    {
                        if (addy.AddressFamily == AddressFamily.InterNetwork)
                        {
                            gotOne = true;
                            ip = addy.ToString();
                            break;
                        }
                    }

                    if (!gotOne)
                    {
                        Log1.Logger("Server.Network").Error("Could not resolve IP address for server " + gsi.Name + " (" + ip + ")");
                        continue;
                    }
                }
                catch (Exception e)
                {
                    Log1.Logger("Server.Network").Error("Error setting up outbound server connection. " + gsi.Name + " / " + gsi.HostName + " : " + e.Message, e);
                    // try the next address in the config
                    continue;
                }

                if (ip.Trim().Length < 1)
                {
                    // try the next address in the config
                    continue;
                }

                gsi.IP = ip;
                gsi.Port = port;
                gsi.IsOnline = false;
                gsi.LastUpdate = DateTime.UtcNow;

                if (OutboundServers.ContainsKey(gsi.UniqueID))
                {
                    continue;
                }
                OutboundServers.Add(gsi.UniqueID, gsi);
            }
            StartOutboundServerUpdate();
        }

        /// <summary>
        /// Does Zeus accept remoe connections?
        /// </summary>
        public static bool AllowRemote { get; set; }

        /// <summary>
        /// Override from ServerBase, to make sure we create the proper connection object for inbound connections.
        /// If we don't override this method, a generic InboundConnection class will be instantiated.
        /// </summary>
        protected override InboundConnection CreateInboundConnection(Socket s, ServerBase server, int serviceID)
        {
            return new ZeusInboundConnection(s, server);
        }

        /// <summary>
        /// Override from ServerBase, to make sure we create the proper connection object for outgoing connections.
        /// If we don't override this method, a generic OutboundServerConnection class will be instantiated.
        /// </summary>
        public override OutboundServerConnection CreateOutboundServerConnection(string name, ServerBase server, string reportedIP, int serviceID)
        {
            return new ZeusOutboundConnection(name, server, reportedIP);
        }

    }
}
