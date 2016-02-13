using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Timers;
using System.Threading;
using PatcherLib;

namespace Shared
{
    public class PatchServerConnection : ClientConnection
    {
        public delegate void DisconnectedDelegate(PatchServerConnection con, string msg);
        /// <summary>
        /// Remote connection closed
        /// </summary>
        public event DisconnectedDelegate Disconnected;

        public delegate void PatchArrivedDelegate(PatchServerConnection con, long fileSize, string fileName);
        /// <summary>
        /// Patch file transmission concluded. 
        /// </summary>
        public event PatchArrivedDelegate PatchArrived;

        public delegate void PatchStreamProgressDelegate(PatchServerConnection con, string patchFileName, long curDownload, long totalDownload);
        /// <summary>
        /// Periodic updates as a patch file is being transmitted
        /// </summary>
        public event PatchStreamProgressDelegate PatchStreamProgress;

        public delegate void PatchNotesDelegate(PatchServerConnection con, string notes);
        /// <summary>
        /// Patch notes arrived from patch server
        /// </summary>
        public event PatchNotesDelegate PatchNotesArrived;

        public delegate void MachineInfoRequestDelegate(bool cpu, bool gpu, bool mainboard, bool drives, bool operatingSystem, bool ram);
        /// <summary>
        /// Server requested machine info.
        /// </summary>
        public event MachineInfoRequestDelegate MachineInfoRequestArrived;

        private string m_PatchNotes = "";
        /// <summary>
        /// The Master patch notes as sent by the server
        /// </summary>
        public string PatchNotes { get { return m_PatchNotes; } }
        private object m_BandwidthSyncRoot = new object();
        
        /// <summary>
        /// Fires in response to a version update request when the version is current.
        /// </summary>
        public event EventHandler ClientIsCurrent;

        /// <summary>
        /// Fires when we're ready to communicate with the server
        /// </summary>
        public event EventHandler ConnectionReady;

        public delegate void MessageToUserDelegate(string msg);

        public PatchServerConnection(bool isBlocking):base(isBlocking)
        {            
            RegisterPacketHandler((int)PacketType.PacketGenericMessage, (int)GenericMessageType.VersionIsCurrent, OnVersionIsCurrent);
            RegisterPacketHandler((int)PacketType.PacketGenericMessage, (int)GenericMessageType.Notes, OnNotesArrived);
            RegisterPacketHandler((int)PacketType.PacketGenericMessage, 878787, OnMachineInfoRequest);
        }

        private void OnMachineInfoRequest(INetworkConnection con, Packet packet)
        {
            bool cpu, gpu, drives, mainboard, os, ram;
            cpu = gpu = drives = mainboard = os = ram = false;

            PacketGenericMessage m = packet as PacketGenericMessage;
            m.ReplyPacket = CreateStandardReply(packet, ReplyType.OK, "");

            if (m.Parms.GetBoolProperty("cpu").GetValueOrDefault(false))
            {
                cpu = true;
                CPUInfo ci = MachineInfo.ReadCPUInfo();
                m.ReplyPacket.Parms.SetProperty("cpu", ci.ToString());
            }
            if (m.Parms.GetBoolProperty("gpu").GetValueOrDefault(false))
            {
                gpu = true;
                VideoCardInfo gi = MachineInfo.ReadGPUInfo();
                m.ReplyPacket.Parms.SetProperty("gpu", gi.ToString());
            }
            if (m.Parms.GetBoolProperty("drives").GetValueOrDefault(false))
            {
                drives = true;
                PatcherLib.DriveInfo di = MachineInfo.ReadLogicalDiskInfo();
                m.ReplyPacket.Parms.SetProperty("drives", di.ToString());
            }
            if (m.Parms.GetBoolProperty("mainboard").GetValueOrDefault(false))
            {
                mainboard = true;
                MotherboardInfo mi = MachineInfo.ReadMotherboardInfo();
                m.ReplyPacket.Parms.SetProperty("mainboard", mi.ToString());
            }
            if (m.Parms.GetBoolProperty("os").GetValueOrDefault(false))
            {
                os = true;
                OperatingSystemInfo osr = MachineInfo.ReadOperatingSystemInfo();
                m.ReplyPacket.Parms.SetProperty("os", osr.ToString());
            }
            if (m.Parms.GetBoolProperty("ram").GetValueOrDefault(false))
            {
                ram = true;
                RamInfo ri = MachineInfo.ReadRAMInfo();
                m.ReplyPacket.Parms.SetProperty("ram", ri.ToString());
            }

            if (MachineInfoRequestArrived != null)
            {
                MachineInfoRequestArrived(cpu, gpu, mainboard, drives, os, ram);
            }
        }

        private void OnVersionIsCurrent(INetworkConnection con, Packet packet)
        {
            if (ClientIsCurrent != null)
            {
                ClientIsCurrent(this, null);
            }
        }

        private void OnNotesArrived(INetworkConnection con, Packet packet)
        {
            PacketGenericMessage msg = packet as PacketGenericMessage;
            m_PatchNotes = msg.Parms.GetStringProperty("notes");
            if (PatchNotesArrived != null)
            {
                PatchNotesArrived((PatchServerConnection)con, PatchNotes);
            }
        }

        protected override void OnFileStreamProgress(string path, long currentBytesDownloaded, long totalBytesToDownload)
        {
            base.OnFileStreamProgress(path, currentBytesDownloaded, totalBytesToDownload);
            if (PatchStreamProgress != null)
            {
                PatchStreamProgress(this, path, currentBytesDownloaded, totalBytesToDownload);
            }
        }

        protected override bool OnFileStreamComplete(string fileName, long totalLengthBytes, int subType, string arg)
        {
            base.OnFileStreamComplete(fileName, totalLengthBytes, subType, arg);            
            if (PatchArrived != null)
            {
                PatchArrived(this, totalLengthBytes, fileName);
            }

            return false;
        }
     

        protected override void OnServerLoginResponse(PacketLoginResult result)
        {
            base.OnServerLoginResponse(result);
            if (result.ReplyCode == ReplyType.OK && ConnectionReady != null)
            {
                ConnectionReady(this, EventArgs.Empty);
            }
        }

        protected override void OnSocketKilled(string msg)
        {
            base.OnSocketKilled(msg);
            if (Disconnected != null)
            {
                Disconnected(this, msg);
            }
        }
    }
}
