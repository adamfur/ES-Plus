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
                var length2 = await reader.ReadAsync(buffer, offset, buffer.Length - offset);

                // if (length2 == 0)
                // {
                //     throw new Exception("if (length2 == 0)");
                // }

                offset += length2;
            } while (offset != buffer.Length);

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