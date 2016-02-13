using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using System.Diagnostics;

namespace PatchGen
{
    class Program
    {
        static void Main(string[] args)
        {

#if !DEBUG
            if (args.Length < 6)
            {                
                Console.WriteLine("Usage:");
                Console.WriteLine("PatchGen.exe <fromVersionNumber> <toVersionNumber> <fromDirectory> <toDirectory> <archivePath> <patchNotes>");
                Console.WriteLine("Arguments with spaces should be surrounded by double quotes.");
                Console.WriteLine("<patchNotes> can also be a text file, denoted with \"file:|FileNameOrFullPath\"");
                Console.ReadLine();
                return;
            }

            string fromVersionNumber = args[0];
            double fver;
            if (!double.TryParse(fromVersionNumber, out fver))
            {
                Console.WriteLine("<fromVersionNumber> must be a number.");
                Console.ReadLine();
                return;
            }
            string toVersionNumber = args[1];
            double tver;
            if (!double.TryParse(toVersionNumber, out tver))
            {
                Console.WriteLine("<toVersionNumber> must be a number.");
                Console.ReadLine();
                return;
            }
            string fromDirectory = args[2];
            string toDirectory = args[3];
            string archivePath = args[4];
            string patchNotes = args[5];
#else
            string fromVersionNumber = "1.0";
            string toVersionNumber = "2.0";
            string fromDirectory = @"C:\PatchGen\1\";
            string toDirectory = @"C:\PatchGen\2\";
            string archivePath = @"C:\PatchGen\patch_" + fromVersionNumber + "_" + toVersionNumber + ".patch";
            string patchNotes = @"Update 2. Deleted some files.";
#endif

            if (patchNotes.Trim().ToLower().StartsWith(@"file:|"))
            {
                patchNotes = patchNotes.Replace(@"file:|", "");
                try
                {
                    if (!File.Exists(patchNotes))
                    {   
                        // check if the file exists in the toDirectory
                        string path = Path.GetFileName(patchNotes);
                        path = Path.Combine(toDirectory, path);
                        if (!File.Exists(path))
                        {
                            Console.WriteLine("Patch notes file " + patchNotes + " does not appear to exist. Can't generate patch.");
                            Console.ReadLine();
                            return;
                        }
                        else
                        {
                            patchNotes = path;
                        }
                    }
                    
                    patchNotes = File.ReadAllText(patchNotes);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error reading patch notes file " + patchNotes + ". " + e.Message);
                    Console.ReadLine();
                }
            }
            PatchGen.PatchMaker.GeneratePatch(fromVersionNumber, toVersionNumber, fromDirectory, toDirectory, archivePath, patchNotes);
            Console.WriteLine("Done.");
            Console.ReadLine();
        }      
    }
}
