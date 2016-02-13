using System;
using System.Collections;
using System.Text;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

namespace Shared
{
    public static class PersistableDiskObject
    {        
        private static bool CreateFileOkay(FileInfo file)
        {
            if (!file.Exists)
            {
                return true;
            }

            return true;
        }

        public static Dictionary<string, byte[]> LoadFromDisk(string filePath)
        {
            FileInfo fi = new FileInfo(filePath);

            // See if the file exists
            if (!fi.Exists)
            {
                return null;
            }

            BinaryReader r = null;
            Dictionary<string, byte[]> data = null;
            try
            {
                r = new BinaryReader(fi.OpenRead());
                data = new Dictionary<string, byte[]>();

                while (data.Count != 10 && r.PeekChar() > -1)
                {
                    string recordName = r.ReadString();
                   // System.Diagnostics.Debug.WriteLine("about to read " + recordName + " from map file.");
                    int len = r.ReadInt32();
                    byte[] bData = r.ReadBytes(len);
                    data.Add(recordName, bData);
                   // System.Diagnostics.Debug.WriteLine("reading " + recordName + " from map file.");
                    if (recordName.ToLower() == "regions")
                    {
                        int x = 0;
                    }
                }
            }
            catch (Exception err)
            {
                return null;
            }
            finally
            {
                if (r != null)
                {
                    r.Close();
                    r = null;
                }
            }

            return data;
        }

        public static bool SaveToDisk(Dictionary<string, byte[]> data, string fulLSaveToPath)
        {
            // write the dang thing to disk:
            FileInfo fi = new FileInfo(fulLSaveToPath);

            // See if the map file exists and whether we should over write it
            if (!CreateFileOkay(fi))
            {
                return false;
            }

            BinaryWriter w = null;
            try
            {
                if (!fi.Directory.Exists)
                {
                    fi.Directory.Create();
                }
                
                w = new BinaryWriter(fi.OpenWrite());
                
                // iterate over the fields and write 'em out
                IDictionaryEnumerator list = data.GetEnumerator();
                while (list.MoveNext())
                {
                    w.Write(list.Key.ToString()); // Record ID
                    object odata = list.Value;

                    if (odata is byte[])
                    {
                        byte[] dat = (byte[])odata;
                        w.Write(dat.Length);
                        w.Write(dat);
                    }
                    if(odata is List<byte>)
                    {
                        List<byte> bData = (List<byte>)odata;
                        w.Write(bData.Count);
                        w.Write(bData.ToArray());
                    }
                    else if (odata is Stream)
                    {
                        Stream binData = (Stream)odata;
                        w.Write((int)binData.Length);
                        byte[] buffer = new byte[256];
                        int read = binData.Read(buffer, 0, buffer.Length);
                        while (read > 0)
                        {
                            w.Write(buffer, 0, read);
                            read = binData.Read(buffer, 0, buffer.Length);
                        }
                    }
                }
            }
            catch (Exception err)
            {
                return false;
            }
            finally
            {
                if (w != null)
                {
                    w.Close();
                    w = null;
                }
            }

            return true;
        }

        
    }
}
