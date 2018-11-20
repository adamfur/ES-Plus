using System;
using System.IO;
using System.Net.Sockets;
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

        public static byte[] ReadBytes(this NetworkStream reader, int length)
        {
            var buffer = new byte[length];
            var offset = 0;
            var remaining = length;

            while (remaining > 0)
            {
                var read = reader.Read(buffer, offset, remaining);

                if (read < 0)
                {
                    // Console.WriteLine($"End of stream reached with {remaining} bytes left to read, ReadTimeout: {reader.ReadTimeout}");
                    throw new EndOfStreamException($"End of stream reached with {remaining} bytes left to read");
                }

                remaining -= read;
                offset += read;
            }

            return buffer;
        }

        public static Int32 ReadInt32(this NetworkStream reader)
        {
            var bytes = ReadBytes(reader, sizeof(Int32));

            return BitConverter.ToInt32(bytes);
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

        public static T ReadStruct<T>(this BinaryReader reader)
        {
            // Console.WriteLine($"ReadStructAsync: {typeof(T).FullName}");
            var size = Marshal.SizeOf(typeof(T));
            var buffer = reader.ReadBytes(size);

            // Pin the managed memory while, copy it out the data, then unpin it
            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            T theStructure = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            handle.Free();

            return theStructure;
        }

        // public static void ReadBytes(this Stream reader, byte[] buffer, int length)
        // {
        //     var offset = 0;

        //     do
        //     {
        //         var length2 = reader.Read(buffer, offset, buffer.Length - offset);

        //         // if (length2 == 0)
        //         // {
        //         //     throw new Exception("if (length2 == 0)");
        //         // }

        //         offset += length2;
        //     } while (offset != buffer.Length);
        // }
    }
}