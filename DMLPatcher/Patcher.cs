using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using ICSharpCode.SharpZipLib.Zip;
using System.IO;
using Shared;
using System.Threading;
using PatcherLib;
using System.Windows;

namespace PatchClient
{    
    /// <summary>
    /// Processes and applies file/directory patch packages
    /// </summary>
    public class Patcher
    {        
        private static PatchServerConnection m_PatchCon;

        /// <summary>
        /// Fires when the patch server tells us that we are at the current version
        /// </summary>
        public static event EventHandler ClientIsCurrent;

        /// <summary>
        /// The server address where we can download the patch chain.
        /// </summary>
        public static string Server = "";

        /// <summary>
        /// The port on which to call the patch server
        /// </summary>
        public static int Port = -1;
        
        /// <summary>
        /// Delegate for the PatchArrived event
        /// </summary>
        /// <param name="patchFile">fully qualified path to the downloaded patch</param>
        /// <param name="fileLength">the length, in bytes, of the patch file</param>
        public delegate void PatchArrivedDelegate(string patchFile, long fileLength);

        /// <summary>
        /// Delegate for the PatchNotesArrived event
        /// </summary>
        /// <param name="notes">Master patch notes</param>
        public delegate void PatchNotesArrivedDelegate(string notes);

        /// <summary>
        /// Delegate for the PatchInfoAdded event
        /// </summary>
        /// <param name="message">Text message for the user. Add to UI.</param>
        public delegate void PatchInfoAddedDelegate(string message);        
        
        /// <summary>
        /// Fires when a patch archive has been successfully downloaded from the patch server.  Once the patch arrives,
        /// it will be automatically applied.
        /// </summary>
        public static event PatchArrivedDelegate PatchArrived;

        /// <summary>
        /// Fires when the master patch notes arrive.  In addition to the master patch notes, each patch file
        /// can also contain additional notes.
        /// </summary>
        public static event PatchNotesArrivedDelegate PatchNotesArrived;

        /// <summary>
        /// Delegate for patchfile download progress
        /// </summary>
        /// <param name="con">the connection that is being used for download</param>
        /// <param name="patchFileName">the path to the file that's being downloaded</param>
        /// <param name="curDownload">how many bytes we've downloaded so far</param>
        /// <param name="totalDownload">how many bytes we are expecting for this file</param>
        public delegate void PatchDownloadProgressDelegate(string patchFileName, long curDownload, long totalDownload);
        
        /// <summary>
        /// Fires about twice a second, while downloading a patch file to give us an update about how much we've downloaded and how much further we have to go.
        /// </summary>
        public static event PatchDownloadProgressDelegate PatchDownloadProgress;

        /// <summary>
        /// Fires when we want to add a new progress message to the UI
        /// </summary>
        public static event PatchInfoAddedDelegate PatchInfoAdded;

        /// <summary>
        /// Delegate for patch processing progress
        /// </summary>
        /// <param name="patchFileName">the path to the file that's being processed</param>
        /// <param name="curProcess">how many differences we've processed</param>
        /// <param name="totalDiffs">how many differences there are total in this patch</param>
        public delegate void PatchApplyProgressDelegate(string patchFileName, long curProcess, long totalDiffs);

        /// <summary>
        /// Fires for every patch difference we apply.
        /// </summary>
        public static event PatchApplyProgressDelegate PatchApplyProgress;

        /// <summary>
        /// Delegate for patch unpacking progress
        /// </summary>
        /// <param name="patchFileName">the path to the file that's being processed</param>
        /// <param name="running">are we still unzipping</param>        
        public delegate void PatchUnzipProgressDelegate(string patchFileName, bool running);

        /// <summary>
        /// Fires for every patch difference we apply.
        /// </summary>
        public static event PatchUnzipProgressDelegate PatchUnzipProgress;


        private static void AddMsg(string msg)
        {
            if (PatchInfoAdded != null)
            {
                PatchInfoAdded(msg);
            }
        }

        /// <summary>
        /// This method must be called before any patching happens.  Sets up the patch directories,
        /// reads the patcher config files and sets up the patch server connection objects.
        /// </summary>
        /// <returns></returns>
        public static bool Initialize()
        {
            string aName = System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName;
            Environment.CurrentDirectory = System.IO.Path.GetDirectoryName(aName);

            try
            {
                if (Directory.Exists(Environment.CurrentDirectory + "\\TEMP\\"))
                {
                    Directory.Delete(Environment.CurrentDirectory + "\\TEMP\\", true);
                }
            }
            catch (Exception de)
            {
                AddMsg("Can't start patcher because we couldn't get a lock on the patch directory: " + de.Message);
                Log.LogMsg("Can't start patcher because we couldn't get a lock on the patch directory: " + de.Message);
                return false;
            }

            // make sure the directory had been deleted
            Thread.Sleep(250);
            Directory.CreateDirectory(Environment.CurrentDirectory + "\\TEMP\\");
            
            // hook up event handlers, ready the networking thread, etc
            ReadyPatchConnection();

            // store the patch server address and port
            Server = ConfigHelper.GetStringConfig("PatchServerAddress");
            Port = ConfigHelper.GetIntConfig("PatchServerPort");
            return true;
        }
        
        private static void ReadyPatchConnection()
        {
            if (m_PatchCon != null)
            {
                m_PatchCon.PatchArrived -= new PatchServerConnection.PatchArrivedDelegate(OnPatchArrived);
                m_PatchCon.Disconnected -= new PatchServerConnection.DisconnectedDelegate(OnDisconnected);
                m_PatchCon.ConnectionReady -= new EventHandler(OnConnectionReady);
                m_PatchCon.ClientIsCurrent -= new EventHandler(OnClientIsCurrent);
                m_PatchCon.PatchStreamProgress -= new PatchServerConnection.PatchStreamProgressDelegate(OnPatchStreamProgress);
                m_PatchCon.PatchNotesArrived -= new PatchServerConnection.PatchNotesDelegate(m_PatchCon_PatchNotesArrived);
                m_PatchCon.SocketConnectionConcluded -= new Action<IClientConnection, bool, string>(m_PatchCon_SocketConnectionConcluded);
            }

            m_PatchCon = new PatchServerConnection(ConfigHelper.GetStringConfig("BlockingMode", "FALSE").ToLower() == "true");
            m_PatchCon.PatchNotesArrived += new PatchServerConnection.PatchNotesDelegate(m_PatchCon_PatchNotesArrived);
            m_PatchCon.SocketConnectionConcluded += new Action<IClientConnection, bool, string>(m_PatchCon_SocketConnectionConcluded);
            m_PatchCon.PatchArrived += new PatchServerConnection.PatchArrivedDelegate(OnPatchArrived);
            m_PatchCon.Disconnected += new PatchServerConnection.DisconnectedDelegate(OnDisconnected);
            m_PatchCon.ConnectionReady += new EventHandler(OnConnectionReady);
            m_PatchCon.ClientIsCurrent += new EventHandler(OnClientIsCurrent);
            m_PatchCon.PatchStreamProgress += new PatchServerConnection.PatchStreamProgressDelegate(OnPatchStreamProgress);
        }

        private static bool m_MasterNotesArrived = false;  
        static void m_PatchCon_PatchNotesArrived(PatchServerConnection con, string notes)
        {
            if (!m_MasterNotesArrived && PatchNotesArrived != null)
            {
                m_MasterNotesArrived = true;
                PatchNotesArrived(notes);
            }
        }

        static void m_PatchCon_SocketConnectionConcluded(IClientConnection arg1, bool arg2, string arg3)
        {
            if (!arg2)
                AddMsg(arg3);
            else
                AddMsg("Connected!");
        }

        private static void OnPatchStreamProgress(PatchServerConnection con, string patchFile, long curDownload, long totalDownload)
        {
            
            if (PatchDownloadProgress != null)
            {
                PatchDownloadProgress(patchFile, curDownload, totalDownload);
            }
        }        
        
        private static void OnClientIsCurrent(object sender, EventArgs e)
        {
            if (ClientIsCurrent != null)
            {
                ClientIsCurrent(null, null);
            }

            CleanupPatchDirectory();
        }

        /// <summary>
        /// The patch notes, either as reported by the server, or as embedded in the patch file
        /// </summary>
        public static string PatchNotes
        {
            get
            {
                if (m_PatchNotes.Length < 1)
                {
                    if (m_PatchCon != null)
                    {
                        return m_PatchCon.PatchNotes;
                    }
                }

                return m_PatchNotes;
            }
        }

        private static string m_PatchNotes = "";

        /// <summary>
        /// Fires after the handshaking process with the patch server has concluded successfully.  Start asking 
        /// for patches as a result of this event firing.
        /// </summary>
        /// <param name="sender">the connection that's ready</param>
        /// <param name="e">always null</param>
        private static void OnConnectionReady(object sender, EventArgs e)
        {
            //AddMsg("Connected!");
            //Log.LogMsg("Connected!");
            RequestVersionUpdate();
        }

        /// <summary>
        /// Initiates a connection the patch server.  If this method return true, the socket was able to connect (i.e. the patch
        /// server was probably up and accepted the connection.  Patcher will start asking for version updates once the client-server
        /// handshake has completed.
        /// </summary>
        /// <returns>if a socket was able to connect, true.</returns>
        public static bool ConnectToPatchServer()
        {
            Log.LogMsg("Connecting to " + Server);
            AddMsg("Connecting to " + Server + ":" + Port);
            m_PatchCon.BeginConnect(Server, Port, "", "");
            return true;
        }

        /// <summary>
        /// Fires when the patch server has been disconnected for any reason
        /// </summary>
        /// <param name="con">the connection that was severed</param>
        /// <param name="msg">a message that might indicate what happened</param>
        private static void OnDisconnected(PatchServerConnection con, string msg)
        {
            AddMsg("Connection closed. " + msg);
            Log.LogMsg("Connection closed.\r\n" + msg);            
            ReadyPatchConnection();
        }

        /// <summary>
        /// Fires when the PatchServerConnection has downloaded a patch file
        /// </summary>
        /// <param name="con">the connection that downloaded the patch</param>
        /// <param name="version">the version that the patch will bring us to, once applied</param>
        /// <param name="path">the full path, on the local disk, to the patch file</param>
        private static void OnPatchArrived(PatchServerConnection con, long fileLength, string fileName)
        {
            Log.LogMsg("Got patch " + Path.GetFileName(fileName) + " .");
            AddMsg("Got patch " + Path.GetFileName(fileName) + " .");                        
            if (PatchArrived != null)
            {
                PatchArrived(fileName, fileLength);
            }
            con.KillConnection("Patch downloaded.");
            Patcher.ProcessPatch(fileName, false);
        }

        /// <summary>
        /// Determines the current version of the installation.
        /// </summary>
        /// <returns>the version number</returns>
        private static string GetCurrentVersion()
        {
            StreamReader fs = null;
            try
            {
                if (!File.Exists("ver.txt"))
                {
                    return "0";
                }

                fs = new StreamReader(new FileStream("ver.txt", FileMode.Open, FileAccess.Read, FileShare.Read));
                return fs.ReadToEnd();
            }
            catch (Exception e)
            {
                return "-1";
            }
            finally
            {
                if (fs != null)
                {
                    fs.Close();
                    fs.Dispose();
                }
            }

            return "0";
        }

        /// <summary>
        /// Extracts the filename only of a file as stored in the relative patch listed in the patch manifest
        /// </summary>
        /// <param name="relativePath">the relative patch listed in the patch manifest</param>
        /// <returns>a file name only</returns>
        private static string GetFileName(string relativePath)
        {
            int idx = relativePath.LastIndexOf("\\");
            if (idx == -1)
            {
                return relativePath;
            }

            return relativePath.Substring(idx);
        }

        /// <summary>
        /// Constructs a full path on the local disk, given the relative path stored in the patch manifest
        /// </summary>
        /// <param name="relativePath">the path stored in the manifest</param>
        /// <returns>full directory path, given the local disk structure</returns>
        private static string GetDirectory(string relativePath)
        {
            int idx = relativePath.LastIndexOf("\\");
            string subDir = "";
            if (idx > -1)
            {
                subDir = relativePath.Substring(0, relativePath.LastIndexOf("\\")) + "\\";
            }

            return Environment.CurrentDirectory + "\\" + subDir;
        }

        /// <summary>
        /// Nukes the temporary ./Patch directory.  Should be the last step in the patch process.
        /// </summary>
        private static void CleanupPatchDirectory()
        {
            try
            {
                Directory.Delete(Environment.CurrentDirectory + "\\TEMP\\", true);
            }
            catch
            {
            }
        }

        /// <summary>
        /// Sends the patch server the version we currently think we are, based on what's in the ver.txt file.
        /// Patch server will send us the next patch in the sequence.
        /// </summary>
        private static void RequestVersionUpdate()
        {
            Log.LogMsg("Requesting version update.");
            AddMsg("Requesting version update.");
            PropertyBag parms = new PropertyBag();
            parms.SetProperty("CurrentVersion", 1, GetCurrentVersion());
            m_PatchCon.SendGenericMessage((byte)GenericMessageType.GetLatestVersion, parms, false);
        }

        /// <summary>
        /// Iterates through the difference list and applies the differences.  Could be file adds, deletes, patches (i.e. binary delta merges) or overwrites.
        /// The files needed for these operatios should be contained in the temporary ./Patch directory.
        /// </summary>
        /// <param name="diffs">the differences to apply</param>
        /// <returns></returns>
        private static bool ApplyDiffs(List<Diff> diffs)
        {
            Log.LogMsg("Applying patch.");
            AddMsg("Applying patch.");
            string patchDir = Environment.CurrentDirectory + "\\TEMP\\";
            try
            {
                if (diffs.Count > 0)
                {
                    if (PatchApplyProgress != null)
                    {
                        PatchApplyProgress(Path.GetFileName(diffs[0].TargetFile), 0, diffs.Count);
                    }
                }

                for (int i = 0; i < diffs.Count; i++)
                {
                    string targetDirectory = GetDirectory(diffs[i].TargetFile);
                    string fileName = GetFileName(diffs[i].TargetFile);

                    switch (diffs[i].Kind)
                    {
                        case DiffType.Add:                                                       
                            if(!Directory.Exists(targetDirectory))
                            {
                                Directory.CreateDirectory(targetDirectory);
                            }
                            
                            File.Copy(patchDir + diffs[i].HashedFilename, targetDirectory + fileName, true);
                            break;
                        case DiffType.Change:
                            if (!ApplyPatch(targetDirectory + fileName, patchDir + diffs[i].HashedFilename + ".patch", targetDirectory + fileName))
                            {
                                return false;
                            }
                            break;
                        case DiffType.Delete:
                            File.Delete(targetDirectory + fileName);
                            if (Directory.GetFiles(targetDirectory).Length < 1)
                            {
                                Directory.Delete(targetDirectory);
                            }
                            break;
                        case DiffType.Overwrite:
                            File.Copy(patchDir + diffs[i].HashedFilename, targetDirectory + fileName, true);
                            break;
                    }

                    if (diffs.Count > 0)
                    {
                        if (PatchApplyProgress != null)
                        {
                            string file = "";
                            if (i + 1 < diffs.Count)
                            {
                                file = Path.GetFileName(diffs[i + 1].TargetFile);
                            }
                            PatchApplyProgress(file, i+1, diffs.Count);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                AddMsg("Patch failed. " + e.Message);
                MessageBox.Show("Patch failed. " + e.Message);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Reads the manifest file which should be included in every .patch file.  
        /// </summary>
        /// <param name="patchFileDirectory">the directory in which the manifest file exists</param>
        /// <returns>the differences indicated in the manifest.</returns>
        private static PatchManifest ReadPatchManifest(string patchFileDirectory)
        {
            PatchManifest manifest = new PatchManifest();

            BinaryReader r = null;            
            try
            {
                FileStream fs = new FileStream(patchFileDirectory + "\\manifest", FileMode.Open, FileAccess.Read, FileShare.Read);
                r = new BinaryReader(fs);

                manifest.FromVersionNumber = r.ReadString();
                manifest.ToVersionNumber = r.ReadString();
                manifest.PatchNotes = r.ReadString();

                while (r.PeekChar() > -1)
                {
                    DiffType dt = (DiffType)r.ReadInt32();
                    string file = r.ReadString();
                    Diff diff = new Diff(dt, file);                    
                    diff.HashedFilename = r.ReadString();
                    manifest.Differences.Add(diff);
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
                }
            }

            return manifest;
        }

        /// <summary>
        /// Unzips a .patch file to the temporary ./Patch directory
        /// </summary>
        /// <param name="patchFile">the .patch file to unzip</param>
        /// <returns></returns>
        private static bool UnzipPatch(string patchFile)
        {
            Log.LogMsg("Decompressing.");
            AddMsg("Decompressing.");

            try
            {
                using (ZipInputStream s = new ZipInputStream(File.OpenRead(patchFile)))
                {
                    ZipEntry theEntry;
                    while ((theEntry = s.GetNextEntry()) != null)
                    {
                        if (PatchUnzipProgress != null)
                        {
                            PatchUnzipProgress(Path.GetFileName(patchFile), true);
                        }

                        Console.WriteLine(theEntry.Name);

                        string directoryName = Path.GetDirectoryName(theEntry.Name);
                        string fileName = Path.GetFileName(theEntry.Name);

                        // create directory
                        if (directoryName.Length > 0)
                        {
                            Directory.CreateDirectory(directoryName);
                        }

                        if (fileName != String.Empty)
                        {
                            using (FileStream streamWriter = File.Create(Environment.CurrentDirectory + "\\TEMP\\" + theEntry.Name))
                            {

                                int size = 2048;
                                byte[] data = new byte[2048];
                                while (true)
                                {
                                    size = s.Read(data, 0, data.Length);
                                    if (size > 0)
                                    {
                                        streamWriter.Write(data, 0, size);
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                return false;
            }
            finally
            {
                if (PatchUnzipProgress != null)
                {
                    PatchUnzipProgress(Path.GetFileName(patchFile), false);
                }
            }
            return true;
        }

        /// <summary>
        /// Applies a binary delta patch to a single target file, given a .patch file
        /// </summary>
        /// <param name="oldFile">the file that will have the patch applied to. the file will be destroyed in its current form and replaced with the patched version</param>
        /// <param name="deltaFile">the .patch delta file that will be applied to the oldFile</param>
        /// <param name="newFile">the new file to be created. can be the same as oldFile or different. either way, oldFile is destroyed</param>
        /// <returns></returns>
        private static bool ApplyPatch(string oldFile, string deltaFile, string newFile)
        {
            try
            {
                // Start the child process.
                Process p = new Process();
                // Redirect the output stream of the child process.
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;

                p.StartInfo.FileName = string.Format(Environment.CurrentDirectory + "\\xdelta3.exe");
                p.StartInfo.Arguments = string.Format(" -d -vfs \"{0}\" \"{1}\" \"{2}\"", oldFile, deltaFile, newFile + ".tmp");
                p.StartInfo.CreateNoWindow = true;
                
                // Apply patch xdelta30.exe -d -vfs OLD_FILE DELTA_FILE DECODED_FILE
                p.Start();

                // Do not wait for the child process to exit before
                // reading to the end of its redirected stream.
                // p.WaitForExit();
                // Read the output stream first and then wait.
                string output = p.StandardOutput.ReadToEnd();
                //Console.WriteLine("--xDelta: " + output);
                p.WaitForExit();

                File.Copy(newFile + ".tmp", newFile, true);
                File.Delete(newFile + ".tmp");

            }
            catch (Exception e)
            {
                AddMsg("Patch failed. Error applying file delta for " + oldFile + ". " + e.Message);
                Console.WriteLine("Error generating file delta for " + oldFile + ". " + e.Message);
                return false;
            }
            
            return true;
        }

        /// <summary>
        /// Unzips a .patch file, reads the patch manifest and applies all of the differences.
        /// This is generally called in response to a PatchServerConnection.PatchArrived event.
        /// </summary>
        /// <param name="path">the patch to the patch file that is to be processed</param>
        private static void ProcessPatch(string path, bool isLocal)
        {
            if (isLocal)
            {
            }
            if (Patcher.UnzipPatch(path) && !isLocal)
            {
                File.Delete(path);
            }
            else
            {
                AddMsg("Unable to unpack patch. Patch file may be corrupt.");
                return;
            }
            string rootPath = Path.GetDirectoryName(path);
            if (isLocal)
            {
                rootPath = Environment.CurrentDirectory + "\\TEMP\\";
            }
            PatchManifest manifest = ReadPatchManifest(rootPath);
           
            if (isLocal)
            {                
                Patcher.m_PatchNotes = manifest.PatchNotes;
            }

            if (PatchNotesArrived != null && manifest.PatchNotes != null && manifest.PatchNotes.Trim().Length > 0)
            {
                PatchNotesArrived(manifest.PatchNotes);
            }

            string curVersion = GetCurrentVersion();
            if(float.Parse(curVersion) != float.Parse(manifest.FromVersionNumber))
            {
                AddMsg("Patch " + Path.GetFileName(path) + " is intended to patch the client FROM version " + manifest.FromVersionNumber + " TO version " + manifest.ToVersionNumber + ".  This client is currently at version " + curVersion + " and can't be patched using this patch file.");
                Log.LogMsg("Patch " + Path.GetFileName(path) + " is intended to patch the client FROM version " + manifest.FromVersionNumber + " TO version " + manifest.ToVersionNumber + ".  This client is currently at version " + curVersion + " and can't be patched using this patch file.");
                CleanupPatchDirectory();
                return;
            }

            if (ApplyDiffs(manifest.Differences))
            {
                CleanupPatchDirectory();
                WriteNewVersion(manifest.ToVersionNumber);
                AddMsg("Patch processed. Now at version " + manifest.ToVersionNumber + ".");
                Log.LogMsg("Patch processed. Now at version " + manifest.ToVersionNumber + ".");

                if (!isLocal)
                {
                    // see if there's a new version
                    ConnectToPatchServer();
                }
                else if (ClientIsCurrent != null)
                {
                    ClientIsCurrent(null, null);
                }
            }
        }

        /// <summary>
        /// Records the current version of the patch
        /// </summary>
        /// <param name="version">the version number to record</param>
        /// <returns></returns>
        private static bool WriteNewVersion(string version)
        {
            StreamWriter sw = null;
            try
            {
                File.Delete("ver.txt");
                sw = new StreamWriter(new FileStream("ver.txt", FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write));
                sw.Write(version);
            }
            catch (Exception e)
            {
                return false;
            }
            finally
            {
                if (sw != null)
                {
                    sw.Close();
                    sw.Dispose();
                }
            }
            return true;
        }

        /// <summary>
        /// Applies a patch file that we have on the local disk.  Do NOT use this method with patches
        /// downloaded from the patch server via ConnectToPatchServer().
        /// </summary>
        /// <param name="patchFile">fully qualified path to the patch server</param>
        public static void ApplyLocalPatch(string patchFile)
        {
            ProcessPatch(patchFile, true);
        }
    }
}
