using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO.Compression;
using System.Timers;
using System.IO;
using Shared;
using System.Threading;

namespace PatchServer
{
    /// <summary>
    /// Represents one GameServerUser connecting to the server. This is a server class.
    /// </summary>
    public class UserConnection : InboundConnection
    {
        public UserConnection(Socket s, ServerBase server, bool isBlocking)
            : base(s, server, isBlocking)
        {
            RegisterPacketHandler((int)PacketType.LoginRequest, OnPlayerLoginRequest);
            RegisterPacketHandler((int)PacketType.PacketGenericMessage, (int)GenericMessageType.GetLatestVersion, OnGetLatestVersion);
            RegisterStandardPacketReplyHandler((int)PacketType.PacketGenericMessage, 878787, OnClientMachineInfo);
        }        

        protected override void OnSocketKilled(string msg)
        {
            ((PatchServerProcess)MyServer).NotifyPatchBytesSent(BytesPatchDataSent);
            ((PatchServerProcess)MyServer).NotifyPatchSent(PatchFileSent);
            base.OnSocketKilled(msg);
        }

        /// <summary>
        /// The number of patch files sent for this connection
        /// </summary>
        public long PatchFileSent { get; set; }

        protected override bool OnFileStreamComplete(string fileName, long totalFileLengthBytes, int subType, string arg)
        {
            PatchFileSent++;
            if(ServerUser != null && ServerUser.MyConnection != null)
            {
                Log1.Logger("Patch").Info("Sent [" + ((int)Util.ConvertBytesToMegabytes(totalFileLengthBytes)).ToString() + "MB] patch to " + ServerUser.MyConnection.RemoteEndPoint.ToString());
            }
            return base.OnFileStreamComplete(fileName, totalFileLengthBytes, subType, arg);
        }

        /// <summary>
        /// The amount of data that has been sent for this connection for the patch
        /// </summary>
        public long BytesPatchDataSent { get; set; }

        protected override void OnFileStreamProgress(string path, long currentBytesDownloaded, long totalBytesToDownload)
        {
            base.OnFileStreamProgress(path, currentBytesDownloaded, totalBytesToDownload);
            BytesPatchDataSent = currentBytesDownloaded;
        }

        /// <summary>
        /// Sends the target connection a listing and status of all game server clusters that this login server knows about.
        /// This is the listing that the client should allow the player too choose from in the UI.
        /// Returns false if currently no servers are available for service.
        /// </summary>
        /// <param name="con"></param>
        protected virtual PacketLoginResult CreateLoginResultPacket()
        {
            // send login result
            PacketLoginResult lr = (PacketLoginResult)CreatePacket((int)PacketType.LoginResult, 0, true, true);
            lr.ReplyCode = ReplyType.OK;
            return lr;
        }

        /// <summary>
        /// Phase 1: Player requests login to the system.  This method attempts to authenticate the player (or create a new account, depending on method parms)
        /// </summary>
        public void OnPlayerLoginRequest(INetworkConnection con, Packet pMsg)
        {
            DoNoAuthLogin(con, pMsg);
        }

        private void DoNoAuthLogin(INetworkConnection con, Packet pMsg)
        {
            try
            {
                PacketLoginRequest p = pMsg as PacketLoginRequest;
                ServerUser.AccountName = Guid.NewGuid().ToString(); // assign random session name
                ServerUser.OwningServer = MyServer.ServerUserID;
                Log1.Logger("LoginServer.Inbound.Login").Info("No-auth assigned user name " + ServerUser.AccountName + " from " + RemoteIP + " is attempting login...");
                Log1.Logger("LoginServer.UserIPMap").Info("User [" + p.AccountName + "] from [" + RemoteIP + "] is attempting login.");
                string msg = "";

                PacketLoginResult result = CreateLoginResultPacket();
                if (result.ReplyCode == ReplyType.OK)
                {
                    ServerUser.AuthTicket = Guid.NewGuid();
                    ServerUser.IsAuthenticated = true;
                    ServerUser.ID = Guid.NewGuid(); // generate random ID for this session

                    ServerUser.Profile.UserRoles = new string[0];
                    result.Parms.SetProperty("AccountName", ServerUser.AccountName);
                    result.Parms.SetProperty(-1, ServerUser.Profile.UserRoles);
                    result.Parms.SetProperty(-2, ServerUser.Profile.MaxCharacters);
                    ConnectionManager.AuthorizeUser(ServerUser);
                }
                pMsg.ReplyPacket = result;
                Log1.Logger("LoginServer.Inbound.Login").Info("Game client *" + ServerUser.AccountName + "* authentication: " + result.ReplyCode.ToString() + ". " + result.ReplyMessage);
            }
            catch (Exception e)
            {
                Log1.Logger("LoginServer.Inbound.Login").Error("Exception thrown whilst player attempted login. " + e.Message, e);
                KillConnection("Error logging in.");
            }
        }

        private bool ShouldCollectClientSpecs()
        {
            return  PatchServerProcess.Instance.CaptureCPU ||
                    PatchServerProcess.Instance.CaptureGPU ||
                    PatchServerProcess.Instance.CaptureMainboard ||
                    PatchServerProcess.Instance.CaptureOS ||
                    PatchServerProcess.Instance.CaptureRAM ||
                    PatchServerProcess.Instance.CaptureDrives;
        }

        private void SendVersionIsCurrent()
        {
            PropertyBag bag = new PropertyBag();
            PacketGenericMessage p = (PacketGenericMessage)CreatePacket((int)PacketType.PacketGenericMessage, (byte)GenericMessageType.VersionIsCurrent, false, false);
            p.Parms = bag;
            p.NeedsDeliveryAck = true;
            Send(p);

            SetTimer(500);// KillConnection("User is current. Disconnecting.");
        }

        private void RequestClientSpec()
        {
            PropertyBag bag = new PropertyBag();
            PacketGenericMessage p = (PacketGenericMessage)CreatePacket((int)PacketType.PacketGenericMessage, 878787, false, false);
            p.Parms = bag;
            p.NeedsDeliveryAck = false;

            if (PatchServerProcess.Instance.CaptureCPU)
            {
                bag.SetProperty("cpu", true);
            }

            if (PatchServerProcess.Instance.CaptureDrives)
            {
                bag.SetProperty("drives", true);
            }

            if (PatchServerProcess.Instance.CaptureGPU)
            {
                bag.SetProperty("gpu", true);
            }

            if (PatchServerProcess.Instance.CaptureMainboard)
            {
                bag.SetProperty("mainboard", true);
            }

            if (PatchServerProcess.Instance.CaptureOS)
            {
                bag.SetProperty("os", true);
            }

            if (PatchServerProcess.Instance.CaptureRAM)
            {
                bag.SetProperty("ram", true);
            }

            Send(p);
        }

        private void OnClientMachineInfo(INetworkConnection con, Packet rep)
        {
            PacketReply p = rep as PacketReply;
            StringBuilder sb = new StringBuilder();
            string ip = "";
            if(con.IsAlive)
            {
                ip = con.RemoteEndPoint.ToString();
            }
            sb.AppendLine("\r\n=-=-= Client Spec [" + ip + "] =-=-=");
            foreach (Property prop in p.Parms.AllProperties)
            {
                sb.AppendLine(prop.StringValue);
            }
            sb.Append("=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=");
            Log1.Logger("UserMetrics").Info(sb.ToString());

            // Let the client go.
            SendVersionIsCurrent();
        }

        private bool m_SentPatchNotes = false;
        public void OnGetLatestVersion(INetworkConnection con, Packet nmsg)
        {
            PacketGenericMessage packet = nmsg as PacketGenericMessage;
            string version = packet.Parms.GetStringProperty("CurrentVersion");

            try
            {
                double curVersion = double.Parse(version);
                int idx = PatchServerProcess.Patches.IndexOfKey(curVersion);
                if (idx == -1)
                {
                    Log1.Logger("Server").Info("User reported being at version number [" + version + "]. Version not known as per Versions.txt. Dropping user connection.");
                    KillConnection("Unknown current version.");
                    return;
                }

                if (!m_SentPatchNotes)
                {
                    PropertyBag bag = new PropertyBag();
                    bag.SetProperty("notes", 1, PatchServerProcess.PatchNotes);
                    PacketGenericMessage p = (PacketGenericMessage)CreatePacket((int)PacketType.PacketGenericMessage, (int)GenericMessageType.Notes, false, false);
                    p.Parms = bag;
                    p.NeedsDeliveryAck = true;
                    Send(p);
                    m_SentPatchNotes = true;                    
                }

                if (idx == (PatchServerProcess.Patches.Count - 1))
                {
                    // if the server is configured to collect client spec data, then we do it right before we tell the client they're current.
                    // if not, just send the iscurrent message now.
                    if (ShouldCollectClientSpecs())
                    {
                        RequestClientSpec();
                    }
                    else
                    {
                        SendVersionIsCurrent();
                    }
                    return;
                }

                // get next patch in line
                idx++;
                FileInfo fi = PatchServerProcess.Patches.Values[idx];                
                try
                {
                    double newVersion = PatchServerProcess.Patches.Keys[idx];
                    SendFileStream(fi, newVersion.ToString());
                }
                catch(Exception se)
                {
                    Log1.Logger("Patcher").Error("Error sending patch to " + RemoteEndPoint.ToString(), se);
                }
            }
            catch(Exception e)
            {
                KillConnection("Malformed patch request. " + e.Message);
                return;
            }

            // we only send one patch file.  let client process that file, then call us back for next file.
            if (Transit.NumAcksWaitingFor < 1)
            {
                KillConnection("Patch sent.");
            }
            else
            {
                SetTimer(500);
            }
        }       

        private void OnDiscTimerElapsed(object sender)
        {
            if (!IsAlive)
            {
                return;
            }

            if (Transit.NumAcksWaitingFor < 1)
            {
                KillConnection("Patch sent.");
            }
            else
            {
                SetTimer(500);
            }
        }

        #region Timer Util

        private System.Threading.Timer m_Timer;

        private void SetTimer(int ms)
        {
            //Log1.Logger("Patcher").Info("Spinning player connection thread. Waiting on " + Transit.NumAcksWaitingFor + " packet acknowledgements.");
            if (ms < 1)
            {
                CancelTimer();
                return;
            }

            if (m_Timer == null)
            {
                m_Timer = new System.Threading.Timer(new TimerCallback(OnDiscTimerElapsed), null, ms, Timeout.Infinite);
            }
            else
            {
                CancelTimer();
                m_Timer.Change(ms, Timeout.Infinite);
            }
        }

        private void CancelTimer()
        {
            if (m_Timer == null)
            {
                return;
            }
            m_Timer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
        }

        #endregion

    }
}
