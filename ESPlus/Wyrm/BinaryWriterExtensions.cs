using System.IO;
using System.Runtime.InteropServices;

namespace ESPlus.Wyrm
{
    public static class BinaryWriterExtensions
    {
        public static void WriteStruct<T>(this BinaryWriter writer, T data)
            where T : struct
        {
            var size = Marshal.SizeOf(data);
            var arr = new byte[size];
            var ptr = Marshal.AllocHGlobal(size);

            Marshal.StructureToPtr(data, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);
            writer.Write(arr, 0, arr.Length);
        }
    }
}