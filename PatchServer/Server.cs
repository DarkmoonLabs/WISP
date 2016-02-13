using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using PatchServer;
using System.Reflection;

namespace Shared
{
	/// <summary>
	/// Basic Server Object.  Listens for connections on a given port
	/// </summary>
	public class PatchServerProcess : ServerBase
	{
        static PatchServerProcess()
        {
        }

        public PatchServerProcess()
        {
            m_Instance = this;
            
            CaptureCPU = ConfigHelper.GetStringConfig("ReportClientCPU", "FALSE").ToLower() == "true";
            CaptureGPU = ConfigHelper.GetStringConfig("ReportClientGPU", "FALSE").ToLower() == "true";
            CaptureDrives = ConfigHelper.GetStringConfig("ReportClientDrives", "FALSE").ToLower() == "true";
            CaptureMainboard = ConfigHelper.GetStringConfig("ReportClientMainBoard", "FALSE").ToLower() == "true";
            CaptureOS = ConfigHelper.GetStringConfig("ReportClientOS", "FALSE").ToLower() == "true";
            CaptureRAM = ConfigHelper.GetStringConfig("ReportClientRAM", "FALSE").ToLower() == "true";

            ReadPatchVersions();
            ReadPatchNotes();


        }

        public bool CaptureCPU { get; set; }
        public bool CaptureGPU { get; set; }
        public bool CaptureDrives { get; set; }
        public bool CaptureMainboard { get; set; }
        public bool CaptureOS { get; set; }
        public bool CaptureRAM { get; set; }

        public void ReadPatchNotes()
        {
            StreamReader r = null;
            try
            {
                string directory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                string file = Path.Combine(directory, "Patches", "PatchNotes.txt");
                if (!File.Exists(file))
                {
                    Log1.Logger("Patcher").Error("Could not locate patch notes file [" + file + "].");
                    return;
                }
                r = new StreamReader(file);
                while (r.Peek() > -1)
                {
                    string line = r.ReadToEnd();
                    PatchNotes = line;
                    Log1.Logger("Patcher").Info("Read patch notes file:\r\n" + PatchNotes);
                }
            }
            catch
            {
            }
            finally
            {
                if (r != null)
                {
                    r.Close();
                    r.Dispose();
                }
            }
        }

        public void ReadPatchVersions()
        {
            PatchServerProcess.Patches.Clear();

            // Add version 0.0 - signifies new install
            PatchServerProcess.Patches.Add(0.0, null);

            StreamReader r = null;
            try
            {
                string directory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                string tfile = Path.Combine(directory, "Patches", "Versions.txt");
                if (!File.Exists(tfile))
                {
                    Log1.Logger("Patcher").Fatal("Could not locate patch version file [" + tfile + "]. Patch server will not be able to distribute patches.");
                    return;
                }
                r = new StreamReader(tfile);
                while (r.Peek() > -1)
                {
                    string line = r.ReadLine().Trim();
                    if (line.StartsWith("--"))
                    {
                        continue;
                    }
                    
                    if (line.Length < 1)
                    {
                        continue;
                    }

                    string[] parts = line.Split(char.Parse(" "));
                    if (parts.Length == 2)
                    {
                        double version = double.Parse(parts[0]);
                        string file = parts[1];
                        string tdirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                        file = Path.Combine(tdirectory, "Patches", file);                
                        if (!File.Exists(file))
                        {
                            Log1.Logger("Patcher").Error("Versions.txt claims that there is a patch file " + file + ", which was not found in the Patches directory.");
                            continue;
                        }

                        // Read the version and found the file. seems like a good patch.
                        Patches.Remove(version);
                        Patches.Add(version, new FileInfo(file));
                        Log1.Logger("Patcher").Info(string.Format("Read patch version {0}.", version));
                    }
                }

                Log1.Logger("Patcher").Info(string.Format("Read {0} total patches.", Patches.Count));
            }
            catch
            {
            }
            finally
            {
                if (r != null)
                {
                    r.Close();
                    r.Dispose();
                }
            }
        }

        /// <summary>
        /// Stores the version numbers and patch file names.  Patch files are expected to be found in ./Patches
        /// </summary>
        public static SortedList<double, FileInfo> Patches = new SortedList<double, FileInfo>();

        /// <summary>
        /// Patch notes sent to the client
        /// </summary>
        public static string PatchNotes = "";

        private static PatchServerProcess m_Instance;
        public static PatchServerProcess Instance
        {
            get
            {
                if (m_Instance == null)
                {
                    m_Instance = new PatchServerProcess();
                }
                return m_Instance;
            }
        }
        
        protected override InboundConnection CreateInboundConnection(Socket s, ServerBase server, int serviceID, bool isBlocking)
        {
            if (serviceID == 7) // 7eus
            {
                ZeusPatchInboundZeusConnection zcon = new ZeusPatchInboundZeusConnection(s, server, isBlocking);
                return zcon;
            }

            UserConnection con = new UserConnection(s, server, isBlocking);
            return con;
        }

        /// <summary>
        /// The number of patch files sent for this connection
        /// </summary>
        public long PatchFileSent = 0;

        /// <summary>
        /// The amount of data that has been sent for this connection for the patch
        /// </summary>
        public long MBytesPatchDataSent = 0;

        public void NotifyPatchSent(long num)
        {
            Interlocked.Add(ref PatchFileSent, num);
        }

        public void NotifyPatchBytesSent(long num)
        {
            Interlocked.Add(ref MBytesPatchDataSent, (long)Util.ConvertBytesToMegabytes(num));
        }

        public override bool StopServer()
        {            
            Log1.Logger("Patch").Info("Shutting down server. This session, sent [" + MBytesPatchDataSent.ToString() + "MB] in patch data across [" + PatchFileSent.ToString() + "] patches.");
            return base.StopServer(); 
        }

    }
}
