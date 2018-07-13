using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ESPlus.Wyrm
{
    public static class StreamExtentions
    {
        public static async Task<byte[]> ReadBytesAsync(this Stream reader, int length)
        {
            var buffer = new byte[length];
            var offset = 0;

            do
            {
                offset += await reader.ReadAsync(buffer, offset, buffer.Length - offset);
            } while (offset != buffer.Length);


            // if (result != buffer.Length)
            // {
            //     throw new Exception($"ReadBytesAsync: wanted {length}, got: {result}");
            // }

            // Console.WriteLine($"ReadBytesAsync : {result}");
            return buffer;
        }

        public static async Task<T> ReadStructAsync<T>(this Stream reader)
        {
            // Console.WriteLine($"ReadStructAsync: {typeof(T).FullName}");
            var size = Marshal.SizeOf(typeof(T));
            var buffer = await reader.ReadBytesAsync(size);

            // Pin the managed memory while, copy it out the data, then unpin it
            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            T theStructure = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            handle.Free();

            return theStructure;
        }
    }
}