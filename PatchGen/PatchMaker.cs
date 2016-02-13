using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.IO;
using System.Diagnostics;
using ICSharpCode.SharpZipLib.Zip;
using PatcherLib;
using Shared;
using System.Text.RegularExpressions;
using System.Linq;

namespace PatchGen
{
    public class PatchMaker
    {
        private static SHA256Managed m_SHA;

        static void DirSearch(string rootDir, string sDir, Dictionary<string, string> files)
        {
            try
            {
                foreach (string f in Directory.GetFiles(sDir))
                {
                    Console.WriteLine(f.Substring(rootDir.Length));
                    files.Add(f.Substring(rootDir.Length), f.Substring(rootDir.Length));
                }

                foreach (string d in Directory.GetDirectories(sDir))
                {
                    DirSearch(rootDir, d, files);
                }
            }
            catch (System.Exception excpt)
            {
                Console.WriteLine(excpt.Message);
            }
        }

        static bool? FilesAreDifferent(string path1, string path2)
        {
            try
            {
                Stream s1 = new FileStream(path1, FileMode.Open, FileAccess.Read, FileShare.Read, 8192);
                byte[] hash1 = m_SHA.ComputeHash(s1);
                s1.Close();

                Stream s2 = new FileStream(path2, FileMode.Open, FileAccess.Read, FileShare.Read, 8192);
                byte[] hash2 = m_SHA.ComputeHash(s2);
                s2.Close();

                if (hash1.Length != hash2.Length)
                {
                    return false;
                }

                for (int i = 0; i < hash1.Length; i++)
                {
                    if (hash1[i] != hash2[i])
                    {
                        return false;
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error checking files " + path1 + " and " + path2 + " for difference: " + e.Message);
                return null;
            }
        }

        static List<Diff> BuildDifferenceList(string prevVer, string curVer, Dictionary<string, string> prevVerFiles, Dictionary<string, string> curVerFiles)
        {
            List<Diff> differences = new List<Diff>();
            foreach (string curVerFile in curVerFiles.Values)
            {
                // if file doesn't exist in previous version, it's an add
                if (!prevVerFiles.ContainsKey(curVerFile))
                {
                    differences.Add(new Diff(DiffType.Add, curVerFile));
                    continue;
                }

                // file exists in previous version. we can remove it from the previous list
                // as long as we do that, everything that's still in the previous list when 
                // we're done looping over the current list is a delete
                prevVerFiles.Remove(curVerFile);

                // Now, see if the file has changed from previous version to
                // current version.
                bool? isSameFile = FilesAreDifferent(curVer + curVerFile, prevVer + curVerFile);
                if (isSameFile == null)
                {
                    Console.WriteLine("Failed to create patch due to file comparison error.");
                    return null;
                }

                if (!(bool)isSameFile)
                {
                    differences.Add(new Diff(DiffType.Change, curVerFile));
                }
            }

            foreach (string dFile in prevVerFiles.Values)
            {
                differences.Add(new Diff(DiffType.Delete, dFile));
            }

            return differences;
        }

       
        private static string CleanFileName(string fileName)
        {
            return Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c.ToString(), string.Empty));
        }

        static bool BuildPatchFiles(string targetTempDir, List<Diff> diffs, string prevDir, string newDir)
        {
            // Make Patch File: xdelta3.exe -e -s  old_file  new_file  delta_file  
            // Apply Patch File: xdelta3.exe -d -s  old_file  delta_file  decoded_new_file
            if (!Directory.Exists(targetTempDir))
            {
                try
                {
                    DirectoryInfo di = Directory.CreateDirectory(targetTempDir);
                    if (!di.Exists)
                    {
                        Console.WriteLine("Failed to create the patch temp directory " + targetTempDir);
                        return false;
                    }
                }
                catch (Exception ce)
                {
                    Console.WriteLine("Failed to create the patch temp directory " + targetTempDir + ". " + ce.Message);
                    return false;
                }
            }

            try
            {                
                for (int i = 0; i < diffs.Count; i++)
                {
                    FileInfo fi = new FileInfo(newDir + diffs[i].TargetFile);

                    string hash = CleanFileName(CryptoManager.GetSHA256Hash(fi.FullName));
                    diffs[i].HashedFilename = hash;

                    switch (diffs[i].Kind)
                    {
                        case DiffType.Add:
                            Console.WriteLine("Adding NEW FILE " + newDir + diffs[i].TargetFile);                                                        
                            fi.CopyTo(Path.Combine(targetTempDir, hash), true);
                            break;
                        case DiffType.Change:
                            Console.WriteLine("Generating DIFF for file " + diffs[i].TargetFile);
                            GenerateFileDelta(prevDir + diffs[i].TargetFile, newDir + diffs[i].TargetFile, targetTempDir + "\\" + hash + ".patch");
                            FileInfo newFile = new FileInfo(newDir + diffs[i].TargetFile);
                            FileInfo patchFile = new FileInfo(targetTempDir + "\\" + hash + ".patch");
                            if (patchFile.Length > newFile.Length)
                            {
                                Diff d = diffs[i];
                                d.Kind = DiffType.Overwrite;
                                fi.CopyTo(Path.Combine(targetTempDir, hash), true);
                                patchFile.Delete();
                            }
                            break;
                        case DiffType.Delete:
                            Console.WriteLine("DELETE file " + diffs[i].TargetFile);
                            break;
                    }
                }

            }
            catch (Exception coe)
            {
                Console.WriteLine("Error generating patch source files: " + coe.Message);
                return false;
            }

            return true;
        }

        static bool GenerateFileDelta(string oldFile, string newFile, string deltaFile)
        {
            try
            {
                // Start the child process.
                Process p = new Process();
                // Redirect the output stream of the child process.
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;

                p.StartInfo.FileName = string.Format(Environment.CurrentDirectory + "\\xdelta3.exe");
                p.StartInfo.Arguments = string.Format(" -9 -S djw -e -vfs \"{0}\" \"{1}\" \"{2}\"", oldFile, newFile, deltaFile);
                // Apply patch xdelta30.exe -d -vfs OLD_FILE DELTA_FILE DECODED_FILE
                p.Start();

                // Do not wait for the child process to exit before
                // reading to the end of its redirected stream.
                // p.WaitForExit();
                // Read the output stream first and then wait.
                string output = p.StandardOutput.ReadToEnd();
                //Console.WriteLine("--xDelta: " + output);
                p.WaitForExit();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error generating file delta for " + oldFile + ". " + e.Message);
                return false;
            }

            return true;
        }

        static void MakePatch(string prevVerDirectory, string curVerDirectory, string fromVersionNum, string toVersionNum, string patchArchive, string patchNotes)
        {
            try
            {
                // Get complete list of previous version's directory files
                Console.WriteLine("Scanning " + prevVerDirectory);
                Dictionary<string, string> prevVerFiles = new Dictionary<string, string>();
                DirSearch(prevVerDirectory, prevVerDirectory, prevVerFiles);

                Console.WriteLine("Scanning " + curVerDirectory);
                Dictionary<string, string> curVerFiles = new Dictionary<string,string>();
                DirSearch(curVerDirectory, curVerDirectory, curVerFiles);

                Console.WriteLine("Building directory difference list.");
                List<Diff> differences = BuildDifferenceList(prevVerDirectory, curVerDirectory, prevVerFiles, curVerFiles);
                if (differences == null || differences.Count < 1)
                {
                    return;
                }

                Console.WriteLine("Done building difference list.  Found " + differences.Count.ToString() + " differences.  Generating file deltas.");
                BuildPatchFiles(Path.Combine(curVerDirectory, "patchFiles"), differences, prevVerDirectory, curVerDirectory);

                Console.WriteLine("Writing manifest.");
                WritePatchManifest(differences, Path.Combine(curVerDirectory, "patchFiles"), fromVersionNum, toVersionNum, patchNotes);

                Console.WriteLine("Packaging patch.");
                ZipPatchDirectory(Path.Combine(curVerDirectory, "patchFiles"), patchArchive);

                // Cleanup
                Console.WriteLine("Deleting temp directory [" + Path.Combine(curVerDirectory, "PatchFiles") + "].");
                Directory.Delete(Path.Combine(curVerDirectory,"PatchFiles"), true);
            }
            catch (System.Exception excpt)
            {
                Console.WriteLine(excpt.Message);
            }
        }

        private static void ZipPatchDirectory(string directory, string patchArchive)
        {
            try
            {
                ConsoleSpinner cs = new ConsoleSpinner();
                // Depending on the directory this could be very large and would require more attention
                // in a commercial package.
                string[] filenames = Directory.GetFiles(directory);

                // 'using' statements guarantee the stream is closed properly which is a big source
                // of problems otherwise.  Its exception safe as well which is great.
                using (ZipOutputStream s = new ZipOutputStream(File.Create(patchArchive)))
                {
                    s.SetLevel(9); // 0 - store only to 9 - means best compression
                    byte[] buffer = new byte[8192];
                    foreach (string file in filenames)
                    {
                        // Using GetFileName makes the result compatible with XP
                        // as the resulting path is not absolute.
                        ZipEntry entry = new ZipEntry(Path.GetFileName(file));

                        // Setup the entry data as required.
                        // Crc and size are handled by the library for seakable streams
                        // so no need to do them here.
                        // Could also use the last write time or similar for the file.
                        entry.DateTime = DateTime.Now;
                        s.PutNextEntry(entry);

                        using (FileStream fs = File.OpenRead(file))
                        {
                            // Using a fixed size buffer here makes no noticeable difference for output
                            // but keeps a lid on memory usage.
                            int sourceBytes;
                            do
                            {
                                cs.Turn();
                                
                                sourceBytes = fs.Read(buffer, 0, buffer.Length);
                                s.Write(buffer, 0, sourceBytes);
                            } while (sourceBytes > 0);
                        }
                    }

                    // Finish/Close arent needed strictly as the using statement does this automatically

                    // Finish is important to ensure trailing information for a Zip file is appended.  Without this
                    // the created file would be invalid.
                    s.Finish();

                    // Close is important to wrap things up and unlock the file.
                    s.Close();
                }
                Console.WriteLine("Patch file [" + patchArchive + "] created.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception during packaging processing {0}", ex);
            }
        }

        private static void WritePatchManifest(List<Diff> differences, string patchFileDirectory, string fromVersion, string toVersion, string patchNotes)
        {
            BinaryWriter w = null;
            try
            {
                FileStream fs = new FileStream(Path.Combine(patchFileDirectory, "manifest"), FileMode.Create, FileAccess.Write, FileShare.Write);
                w = new BinaryWriter(fs);

                w.Write(fromVersion);
                w.Write(toVersion);
                w.Write(patchNotes);

                foreach (Diff d in differences)
                {
                    w.Write((int)d.Kind);
                    w.Write(d.TargetFile);
                    w.Write(d.HashedFilename);
                }
            }
            catch
            {
            }
            finally
            {
                if (w != null)
                {
                    w.Close();
                }
            }
        }

        // Entry point
        public static void GeneratePatch(string fromVersionNumber, string toVersionNumber, string fromDirectory, string toDirectory, string archivePath, string patchNotes)
        {
            m_SHA = new SHA256Managed();
            
            //if (args.Length < 3)

            if (!Directory.Exists(fromDirectory))
            {
                Console.WriteLine("Directory does not exist: " + fromDirectory);
                Console.ReadLine();
                return;
            }

            if (!Directory.Exists(toDirectory))
            {
                Console.WriteLine("Directory does not exist: " + toDirectory);
                Console.ReadLine();
                return;
            }

            if (File.Exists(archivePath))
            {
                Console.WriteLine("Patch archive file already exists: " + archivePath);
                Console.ReadLine();
                return;
            }

            if (Directory.Exists(Path.Combine(toDirectory,"PatchFiles")))
            {
                Directory.Delete(Path.Combine(toDirectory, "PatchFiles"), true);
            }

            DateTime start = DateTime.Now;
            MakePatch(fromDirectory, toDirectory, fromVersionNumber, toVersionNumber, archivePath, patchNotes);
            DateTime end = DateTime.Now;
            TimeSpan duration = end - start;
            Console.WriteLine("Patch archive process complete in " + duration.ToString());
            Console.WriteLine("Press <ENTER> to continue.");
            Console.ReadLine();
        }

    }
}
