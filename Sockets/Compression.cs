using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Shared
{
    /// <summary>
    /// Compression helper class
    /// </summary>
    public class Compression
    {

        public static byte[] ReadFullStream(Stream stream)
        {
            byte[] buffer = new byte[32768];
            using (MemoryStream ms = new MemoryStream())
            {
                while (true)
                {
                    int read = stream.Read(buffer, 0, buffer.Length);
                    if (read <= 0)
                        return ms.ToArray();
                    ms.Write(buffer, 0, read);
                }
            }
        }

        public static byte[] DecompressData(byte[] bytDs, int dataStart)
        {
            /*
            MemoryStream inMs = new MemoryStream(bytDs);
            //DeflateStream zipStream = new DeflateStream(inMs, CompressionMode.Decompress, true);
            ICSharpCode.SharpZipLib.GZip.GZipInputStream zipStream = new ICSharpCode.SharpZipLib.GZip.GZipInputStream(inMs);
            byte[] outByt = ReadFullStream(zipStream);
            zipStream.Flush();
            zipStream.Close();
            return outByt;
             * */

            return bytDs;
        }


        public static byte[] CompressData(byte[] data, Pointer p)
        {          
            /*
            MemoryStream objStream = new MemoryStream();
            //DeflateStream objZS = new System.IO.Compression.DeflateStream(objStream, CompressionMode.Compress);
            ICSharpCode.SharpZipLib.GZip.GZipOutputStream objZS = new ICSharpCode.SharpZipLib.GZip.GZipOutputStream(objStream);
            objZS.Write(data, 0, data.Length);
            objZS.Flush();
            objZS.Close();
            return objStream.ToArray();
             * */

            return data;
        }

    }
}
