using System;
using System.Net;
using System.Runtime.InteropServices;

public class Util
{
    /// <summary>
    /// Utility function that converts bytes to megabytes
    /// </summary>
    public static float ConvertBytesToMegabytes(UInt64 bytes)
    {
        return (bytes / 1024f) / 1024f;
    }

    /// <summary>
    /// Utility function that converts bytes to megabytes
    /// </summary>
    public static float ConvertBytesToMegabytes(long bytes)
    {
        return (bytes / 1024f) / 1024f;
    }

    /// <summary>
    /// Utility function to convert kilobytes to megabytes
    /// </summary>
    public static float ConvertKilobytesToMegabytes(UInt64 kilobytes)
    {
        return kilobytes / 1024f;
    }
    
#if SILVERLIGHT || UNITY_WEB
    public static void Copy(byte[] source, int sourceOffset, byte[] target, int targetOffset, int count)
    {
        Buffer.BlockCopy(source, sourceOffset, target, targetOffset, count);
    }
#else
    static readonly int sizeOfInt = Marshal.SizeOf(typeof(long));
    public static unsafe void Copy(byte[] source, int sourceOffset, byte[] target, int targetOffset, int count)
    {
        // The following fixed statement pins the location of the src and dst objects
        // in memory so that they will not be moved by garbage collection.
        // The following fixed statement pins the location of the source and
        // target objects in memory so that they will not be moved by garbage
        // collection.
        fixed (byte* pSource = source, pTarget = target)
        {
            // Set the starting points in source and target for the copying.
            byte* ps = pSource + sourceOffset;
            byte* pt = pTarget + targetOffset;

            // Copy the specified number of bytes from source to target.
            for (int i = 0; i < count / sizeOfInt; i++)
            {
                *((long*)pt) = *((long*)ps);
                pt += sizeOfInt;
                ps += sizeOfInt;
            }

            // Complete the copy by moving any bytes that weren't moved in blocks of 4:
            for (int i = 0; i < count % sizeOfInt; i++)
            {
                *pt = *ps;
                pt++;
                ps++;
            }

        }


    }
#endif
}

