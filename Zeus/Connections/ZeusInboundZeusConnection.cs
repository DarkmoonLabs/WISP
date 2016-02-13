using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Configuration;
using ServerLib;
using System.Web.Security;
using Zeus;
using Microsoft.Win32;
using System.IO;

namespace Shared
{
    /// <summary>
    /// Represents one other server in the Hive connecting to this server.
    /// </summary>
    public class ZeusInboundZeusConnection : ZeusInboundConnection
    {
        public ZeusInboundZeusConnection(Socket s, ServerBase server, bool isBlocking)
            : base(s, server, isBlocking)
        {
            // Register Packet handlers that should be accepted for all connection, regardless if they are authenticated or not.
            // use "RegisterPacketHandler(..."
        }

        /// <summary>
        /// Override any methods you like to catch various events.  OnSocketKilled is a common one. OnClusterServerLoginResolved is also a commonly used one.
        /// </summary>
        /// <param name="msg"></param>
        protected override void OnSocketKilled(string msg)
        {
            base.OnSocketKilled(msg);
        }

        #region Performance Monitors

        private void OnPerfMonCounterDataRequest(INetworkConnection con, Packet r)
        {
            Log1.Logger("Zeus.Inbound.Client").Debug("Performance Monitor Counter data request from " + ServerUser.AccountName + ".");

            PacketGenericMessage msg = r as PacketGenericMessage;

            r.ReplyPacket = CreateStandardReply(r, ReplyType.OK, "");
            string[] counters = msg.Parms.GetStringArrayProperty(2);
            DateTime[] lastCheck = msg.Parms.GetDateTimeArrayProperty(3);

            msg.ReplyPacket.Parms.SetProperty(3, DateTime.UtcNow);

            List<PerfHistory> clientHistories = new List<PerfHistory>();
            for (int i = 0; i < counters.Length; i++)
            {
                PerfHistory serverHistory = null;
                if (PerfMon.History.TryGetValue(counters[i], out serverHistory))
                {
                    Log.LogMsg("Got request for perf log " + serverHistory.Key + " since " + lastCheck[i].ToLongTimeString());
                    PerfHistory clientHis = new PerfHistory(serverHistory);
                    List<PerfHistory.HistoryItem> buff = new List<PerfHistory.HistoryItem>();
                    for (int x = serverHistory.History.Count - 1; x > -1; x--)
                    {
                        if (serverHistory.History[x].Timestamp > lastCheck[i])
                        {
                            //Log.LogMsg("Adding perf item " + ph.Key + " with timestamp " + ph.History[x].Timestamp.ToLongTimeString());
                            buff.Add(serverHistory.History[x]);
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (buff.Count > 0)
                    {
                        clientHistories.Add(clientHis);
                        buff.Reverse();
                        clientHis.History.AddRange(buff);
                        clientHis.HelpText += " (" + clientHis.History[clientHis.History.Count - 1].Value + ")";
                        Log.LogMsg("Last timestamp sent for perf " + clientHis.Key + " is " + clientHis.LastSample.ToLongTimeString());
                    }
                }
            }

            msg.ReplyPacket.Parms.SetProperty(2, clientHistories.ToArray());
        }

        private void OnPerfMonCounterOverviewRequest(INetworkConnection con, Packet r)
        {
            Log1.Logger("Zeus").Debug("Performance Monitor Counter overview request from " + ServerUser.AccountName + ".");

            PacketGenericMessage msg = r as PacketGenericMessage;

            r.ReplyPacket = CreateStandardReply(r, ReplyType.OK, "");
            PerfHistory[] history = new PerfHistory[PerfMon.History.Count];
            PerfMon.History.Values.CopyTo(history, 0);
            msg.ReplyPacket.Parms.SetProperty(2, history);
            msg.ReplyPacket.Parms.SetProperty(3, PerfMon.IsSampling);
            
        }

        private void OnPerfMonStartRequest(INetworkConnection con, Packet r)
        {
            Log1.Logger("Zeus").Debug("Performance Monitor Start request from " + ServerUser.AccountName + ".");

            if (!ServerUser.Profile.IsUserInRole("Administrator"))
            {
                Log1.Logger("Zeus").Warn("[" + ServerUser.AccountName + "] has insufficient permissions to start performance monitor.");
                r.ReplyPacket = CreateStandardReply(r, ReplyType.Failure, "Insufficient permissions. Only Administrators can start the performance monitor.");
                return;
            }

            PacketGenericMessage msg = r as PacketGenericMessage;
         
            r.ReplyPacket = CreateStandardReply(r, ReplyType.OK, "");
            string[] counters = msg.Parms.GetStringArrayProperty(2);

            if (counters.Length == 0)
            {
                r.ReplyPacket.ReplyCode = ReplyType.Failure;
                r.ReplyPacket.ReplyMessage = "Must specify at least one counter to start.";
                return;
            }

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < counters.Length; i++)
            {
                string key = counters[i];
                PerfHistory h = null;
                if (PerfMon.History.TryGetValue(key, out h))
                {
                    h.IsEnabled = true;
                    if (sb.Length == 0)
                    {
                        sb.Append("Enabled performance counter ");
                    }
                    sb.Append(key.Replace("|", " -> ") + ", ");
                }
            }

            if (sb.Length >= 2)
            {
                sb.Remove(sb.Length - 2, 1);
                PerfMon.InstallCounters();
                PerfMon.StartSamplingCounters();
            }
            else if (sb.Length == 0)
            {
                r.ReplyPacket.ReplyCode = ReplyType.Failure;
                sb.Append("No performance counters were activated.");
            }

            r.ReplyPacket.ReplyMessage = sb.ToString();
           
        }
        #endregion

        #region Services

        private void OnChangeServiceStateRequest(INetworkConnection con, Packet r)
        {
            Log1.Logger("Zeus").Debug("Service state change request from " + ServerUser.AccountName + ".");
            if (!ServerUser.Profile.IsUserInRole("Administrator"))
            {
                Log1.Logger("Zeus").Warn("[" + ServerUser.AccountName + "] has insufficient permissions to change service state.");
                r.ReplyPacket = CreateStandardReply(r, ReplyType.Failure, "Insufficient permissions. Only Administrators can change service states.");
                return;
            }

            PacketGenericMessage msg = r as PacketGenericMessage;
            r.ReplyPacket = CreateStandardReply(r, ReplyType.OK, "");

            string service = msg.Parms.GetStringProperty(2);
            string targetState = msg.Parms.GetStringProperty(3);
            string result = "";
            Log1.Logger("Zeus").Debug(ServerUser.AccountName + " requested that [" + service + "] change state to [" + targetState + "].");

            if (targetState.ToLower() == "start")
            {
                result = WispServices.StartWispService(service);
            }
            else if (targetState.ToLower() == "stop")
            {
                result = WispServices.StopWispService(service);
            }
            else if (targetState.ToLower() == "uninstall")
            {
                result = WispServices.UninstallService(service);
                //int idx = MyServer.OutboundServerGroups["Default"].OutboundServers.Find(s=>s.n
            }

            r.ReplyPacket.ReplyMessage = result;

            Log1.Logger("Zeus").Debug(ServerUser.AccountName + " requested service state change for [" + service + "] to state [" + targetState + "] result: [" + result + "].");
            string[] services = WispServices.GetInstalledServices();
            msg.ReplyPacket.Parms.SetProperty(2, services);
        }    

        private void OnServiceOverviewRequest(INetworkConnection con, Packet r)
        {
            Log1.Logger("Zeus").Debug("Service overview request from " + ServerUser.AccountName + ".");
            PacketGenericMessage msg = r as PacketGenericMessage;
            r.ReplyPacket = CreateStandardReply(r, ReplyType.OK, "");
            string[] services = WispServices.GetInstalledServices();
            msg.ReplyPacket.Parms.SetProperty(2, services);
        }

        protected override bool OnFileStreamComplete(string fileName, long totalFileLengthBytes, int subType, string arg)
        {
            if (!ServerUser.Profile.IsUserInRole("Administrator"))
            {
                Log1.Logger("Zeus").Error("Received file transfer from unauthorized user [" + ServerUser.AccountName + "].");
                SendGenericMessage((int)GenericMessageType.ServiceInstallResult, "Received file transfer from unauthorized user [" + ServerUser.AccountName + "].", false);
                return true;
            }

            bool rslt = base.OnFileStreamComplete(fileName, totalFileLengthBytes, subType, arg);

            if (subType != 1) // install service
            {
                Log1.Logger("Zeus").Error("Received file transfer with unexpected reason [" + subType + "], [arg=" + arg + "].");
                SendGenericMessage((int)GenericMessageType.ServiceInstallResult, "Received file transfer with unexpected reason [" + subType + "], [arg=" + arg + "].", false);
                // can't install the service. just delete the temp file.
                return true;
            }

            string[] parts = arg.Split(char.Parse("|"));
            if (parts.Length < 3)
            {
                Log1.Logger("Zeus").Error("Service description was malformed. Can't install new service. [" + arg + "].");
                SendGenericMessage((int)GenericMessageType.ServiceInstallResult, "Service description was malformed. Can't install new service. [" + arg + "].", false);
                // can't install the service. just delete the temp file.
                return true;
            }

            string name = parts[1];
            string desc = parts[2];
            string serverExe = parts[3];

            if (name.Length < 1 || desc.Length < 1 || serverExe.Length < 1)
            {
                Log1.Logger("Zeus").Error("Service description was malformed. Can't install new service. [Name=" + name + "], [Desc=" + desc + "].");
                SendGenericMessage((int)GenericMessageType.ServiceInstallResult, "Service description was malformed. Can't install new service. [Name=" + name + "], [Desc=" + desc + "].", false);
                // can't install the service. just delete the temp file.
                return true;
            }

            string deployPath = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Wisp\", "TargetDir", Environment.CurrentDirectory);
            if (deployPath == null || deployPath.Length < 1)
            {
                Log1.Logger("Zeus").Error("Unable to unzip new Wisp server directory as the Wisp install directory is not set in the registry.  Re-run the Wisp Deployer utility on this machine to fix that problem.");
                SendGenericMessage((int)GenericMessageType.ServiceInstallResult, "Unable to unzip new Wisp server directory as the Wisp install directory is not set in the registry.  Re-run the Wisp Deployer utility on this machine to fix that problem.", false);
                return true;
            }

            if (Array.IndexOf(WispServices.GetInstalledServices(), name) > -1)
            {
                Log1.Logger("Zeus").Error("Unable to install new Wisp server [" + name + "] because a Wisp service by that name already exists on this machine.");
                SendGenericMessage((int)GenericMessageType.ServiceInstallResult, "Unable to install new Wisp server [" + name + "] because a Wisp service by that name already exists on this machine.", false);
                return true;    
            }

            if (!Directory.Exists(deployPath))
            {
                Directory.CreateDirectory(deployPath);
                if (!Directory.Exists(deployPath))
                {
                    Log1.Logger("Zeus").Error("Unable to unzip new Wisp server directory as the Wisp install directory does not exist and could not be created. [" + deployPath + "].");
                    SendGenericMessage((int)GenericMessageType.ServiceInstallResult, "Unable to unzip new Wisp server directory as the Wisp install directory does not exist and could not be created. [" + deployPath + "].", false);
                    return true;
                }
            }

            string newServerRoot = Path.Combine(deployPath.TrimEnd(new char[] { '\\' }), "Server", name);

            if (Directory.Exists(newServerRoot))
            {
                bool errDel = false;    
                try
                {
                }
                catch
                {
                    errDel = true;
                    return true;
                }
             
                if (Directory.Exists(newServerRoot) || errDel)
                {
                    Log1.Logger("Zeus").Error("Can't install new Wisp service as a service by that name as it already exists on disk. [" + name + "]");
                    SendGenericMessage((int)GenericMessageType.ServiceInstallResult, "Can't install new Wisp service as a service by that name as it already exists on disk. [" + name + "]", false);
                    return true;
                }
            }

            if (!Zeus.Util.UnzipFile(fileName, newServerRoot))
            {
                Log1.Logger("Zeus").Error("Unzip failure. Can't install new Wisp server.");
                SendGenericMessage((int)GenericMessageType.ServiceInstallResult, "Unzip failure. Can't install new Wisp server.", false);
                return true;
            }

            if (!File.Exists(Path.Combine(newServerRoot, serverExe)))
            {
                Log1.Logger("Zeus").Error("Failed to install new Wisp server.  The indicated exe [" + serverExe + "] does not exist in the server ZIP package.");
                SendGenericMessage((int)GenericMessageType.ServiceInstallResult, "Failed to install new Wisp server.  The indicated exe [" + serverExe + "] does not exist in the server ZIP package.", false);
                return true;
            }

            if (!WispServices.InstallService(Path.Combine(newServerRoot, serverExe), name, desc))
            {
                Log1.Logger("Zeus").Error("Failed to install new Wisp server service.");
                SendGenericMessage((int)GenericMessageType.ServiceInstallResult, "Failed to install new Wisp server service.", false);
                return true;
            }

            Log1.Logger("Zeus").Info("Installed new Wisp server service [" + name + "] at [" + serverExe + "] on the authority of [" + ServerUser.AccountName + "].");
            SendGenericMessage((int)GenericMessageType.ServiceInstallResult, "Installed new Wisp server service [" + name + "]!", false);

            return rslt;
        }

        #endregion        

        protected override void LoggedInAndReady()
        {
            base.LoggedInAndReady();

            RegisterPacketHandler((int)PacketType.PacketGenericMessage, (int)GenericZeusPacketType.RequestPerfMonStart, OnPerfMonStartRequest);
            RegisterPacketHandler((int)PacketType.PacketGenericMessage, (int)GenericZeusPacketType.RequestPerfMonCounterOverview, OnPerfMonCounterOverviewRequest);
            RegisterPacketHandler((int)PacketType.PacketGenericMessage, (int)GenericZeusPacketType.RequestPerfMonCounterData, OnPerfMonCounterDataRequest);
            RegisterPacketHandler((int)PacketType.PacketGenericMessage, (int)GenericZeusPacketType.RequestServiceOverview, OnServiceOverviewRequest);
            RegisterPacketHandler((int)PacketType.PacketGenericMessage, (int)GenericZeusPacketType.RequestChangeServiceState, OnChangeServiceStateRequest);            
        }

    }
}
