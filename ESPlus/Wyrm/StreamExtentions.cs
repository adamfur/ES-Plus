using System;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace ESPlus.Wyrm
{
    public static class StreamExtentions
    {
        public static async Task<byte[]> ReadBytesAsync(this Stream stream, int length)
        {
            var buffer = new byte[length];
            var offset = 0;
            var remaining = length;

            while (remaining > 0)
            {
                var result = await stream.ReadAsync(buffer, offset, remaining);

                if (result < 0)
                {
                    throw new Exception($"ReadBytes, result: {result}");
                }

                offset += result;
                remaining -= result;
            }

            return buffer;
        }

        public static async Task<T> ReadStructAsync<T>(this Stream reader)
        {
            // Console.WriteLine($"ReadStructAsync: {typeof(T).FullName}");
            var size = Marshal.SizeOf(typeof(T));
            var buffer = await reader.ReadBytesAsync(size);

            // Pin the managed memory while, copy it out the data, then unpin it
            var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            var theStructure = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));

            handle.Free();

            return theStructure;
        }

        public static T ReadStruct<T>(this BinaryReader reader)
        {
            // Console.WriteLine($"ReadStructAsync: {typeof(T).FullName}");
            var size = Marshal.SizeOf(typeof(T));
            var buffer = reader.ReadBytes(size);

            // Pin the managed memory while, copy it out the data, then unpin it
            var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            var theStructure = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));

            handle.Free();

            return theStructure;
        }
    }
}