using System;
using System.IO;
using System.Runtime.InteropServices;

namespace ESPlus.Wyrm
{
    public static class BinaryWriterExtentions
    {
        public static void WriteStruct<T>(this BinaryWriter writer, T data)
            where T : struct
        {
            int size = Marshal.SizeOf(data);
            byte[] arr = new byte[size];
            IntPtr ptr = Marshal.AllocHGlobal(size);

            Marshal.StructureToPtr(data, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);
            writer.Write(arr, 0, arr.Length);
        }
    }
}