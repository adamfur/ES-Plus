using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ESPlus.Repositories;
using EventStore.ClientAPI;

namespace ESPlus
{
    public class WyrmEvent2
    {
        public long Offset { get; set; }
        public long TotalOffset { get; set; }
        public Guid EventId { get; set; }
        public long Version { get; set; }
        public DateTime Timestamp { get; set; }
        public byte[] Metadata { get; set; }
        public byte[] Data { get; set; }
        public ulong EventTypeHash { get; set; }
    }

    public static class BinaryReaderExtentions
    {
        public static T FromBinaryReader<T>(this BinaryReader reader)
        {

            // Read in a byte array
            byte[] bytes = reader.ReadBytes(Marshal.SizeOf(typeof(T)));

            // Pin the managed memory while, copy it out the data, then unpin it
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            T theStructure = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            handle.Free();

            return theStructure;
        }
    }

    public static class StreamExtentions
    {
        public static async Task<byte[]> ReadBytesAsync(this Stream reader, int length)
        {
            var buffer = new byte[length];
            var result = await reader.ReadAsync(buffer, 0, buffer.Length);

            if (result != buffer.Length)
            {
                throw new Exception("public static async Task<T> ReadStructAsync<T>(this StreamReader reader)");
            }

            return buffer;
        }

        public static async Task<T> ReadStructAsync<T>(this Stream reader)
        {
            var size = Marshal.SizeOf(typeof(T));
            var buffer = await reader.ReadBytesAsync(size);

            // Pin the managed memory while, copy it out the data, then unpin it
            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            T theStructure = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            handle.Free();

            return theStructure;
        }
    }

    public class WyrmConnection
    {
        private byte[] Combine(params byte[][] arrays)
        {
            byte[] rv = new byte[arrays.Sum(a => a.Length)];
            int offset = 0;
            foreach (byte[] array in arrays)
            {
                System.Buffer.BlockCopy(array, 0, rv, offset, array.Length);
                offset += array.Length;
            }
            return rv;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct Position
        {
            public Int64 A;
            public Int64 B;
            public Int64 C;
            public Int64 D;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct Monkey
        {
            public UInt64 EventTypeHash;
            public Position Position;
            public Int64 Offset;
            public Int64 TotalOffset;
            public Guid EventId;
            public Int64 Version;
            public Int64 UncompressedSize;
            public Int64 CompressedSize;
            public Int64 EncryptedSize;
            public Int64 Clock;
            public Int64 Ms;
        }

        public IEnumerable<WyrmEvent2> EnumerateStream(string streamName)
        {
            var client = new TcpClient();

            client.ConnectAsync("localhost", 8888).Wait();

            var stream = client.GetStream();
            var writer = new BinaryWriter(client.GetStream());
            var name = Encoding.UTF8.GetBytes(streamName);
            writer.Write(OperationType.READ_STREAM_FORWARD);
            writer.Write(name.Length);
            writer.Write(name, 0, name.Length);
            writer.Write((int)0); //filter
            writer.Flush();

            while (true)
            {
                var length = stream.ReadStructAsync<Int64>().Result;

                if (length == 8)
                {
                    Console.WriteLine("reached end!");
                    break;
                }

                var monkey = stream.ReadStructAsync<Monkey>().Result;
                var eventTypeHash = monkey.EventTypeHash;
                var position = monkey.Position;
                var epooch = new DateTime(1970, 1, 1);
                var time = epooch.AddSeconds(monkey.Clock).AddMilliseconds(monkey.Ms).ToLocalTime();
                var metadata = new byte[0];
                var data = new byte[0];
                var compressed = stream.ReadBytesAsync((int)monkey.CompressedSize).Result;
                var uncompressed = new byte[monkey.UncompressedSize];
                var result = LZ4.LZ4_decompress_safe(compressed, uncompressed, compressed.Length, uncompressed.Length);

                using (var mx = new MemoryStream(uncompressed))
                {
                    mx.Seek(0, SeekOrigin.Begin);
                    using (var ms2 = new BinaryReader(mx))
                    {
                        var lengthOfMetadata = ms2.ReadInt32();
                        var lengthOfData = ms2.ReadInt32();

                        metadata = ms2.ReadBytes(lengthOfMetadata);
                        data = ms2.ReadBytes(lengthOfData);
                    }
                }

                yield return new WyrmEvent2
                {
                    Offset = monkey.Offset,
                    TotalOffset = monkey.TotalOffset,
                    EventId = monkey.EventId,
                    Version = monkey.Version,
                    Timestamp = time,
                    Metadata = metadata,
                    Data = data,
                    EventTypeHash = eventTypeHash
                };
            }

            yield break;
        }

        public async Task DeleteAsync(string streamName)
        {
            var client = new TcpClient();

            await client.ConnectAsync("localhost", 8888);

            var reader = new BinaryReader(client.GetStream());
            var writer = new BinaryWriter(client.GetStream());
            var name = Encoding.UTF8.GetBytes(streamName);
            writer.Write(OperationType.DELETE);
            writer.Write(name.Length);
            writer.Write(name, 0, name.Length);

            var len = reader.ReadInt32();

            if (len != 8)
            {
                throw new Exception("if (len != 8)");
            }

            var status = reader.ReadInt32();

            if (status != 0)
            {
                throw new Exception($"if (status != 0): {status}");
            }
            await Task.FromResult(0);
        }

        public async Task Append(IEnumerable<WyrmEvent> events)
        {
            var client = new TcpClient();

            await client.ConnectAsync("localhost", 8888);
            var reader = new BinaryReader(client.GetStream());
            var writer = new BinaryWriter(client.GetStream());
            var concat = Combine(events.Select(x => Assemble(x)).ToArray());
            int length = concat.Length;

            writer.Write(OperationType.PUT);
            writer.Write(length);
            writer.Write(concat, 0, length);
            writer.Flush();

            var len = reader.ReadInt32();

            if (len != 8)
            {
                throw new Exception("if (len != 8)");
            }

            var status = reader.ReadInt32();

            if (status != 0)
            {
                throw new WrongExpectedVersionException($"if (status != 0): {status}");
            }
            await Task.FromResult(0);
        }

        // bool first = true;
        private byte[] Assemble(WyrmEvent @event)
        {
            var target = new MemoryStream();
            var writer = new BinaryWriter(target); //tmp .vs networkStream

            var streamName = Encoding.UTF8.GetBytes(@event.StreamName);
            var eventType = Encoding.UTF8.GetBytes(@event.EventType);
            var version = @event.Version;
            var metadata = @event.Metadata;
            var body = @event.Body;
            var eventId = @event.EventId;
            var uncompressed = BuildPayload(metadata, body);
            var uncompressedLength = uncompressed.Length;
            var compressed = new byte[LZ4.LZ4_compressBound(uncompressedLength)];
            var compressedLength = LZ4.LZ4_compress_default(uncompressed, compressed, uncompressedLength, compressed.Length);
            // if (first)
            //  for (int i = 0; i < compressedLength; ++i)
            //     {
            //         var c = compressed[i];
            //         Console.WriteLine("compress: " + (int)c + " " + (char)c);
            //     }
            // first = false;

            // Console.WriteLine($"Append: compressedLength {compressedLength}");
            var length = compressedLength + 40 + streamName.Length + eventType.Length;

            writer.Write(length); //4
            writer.Write(streamName.Length); //8
            writer.Write(eventType.Length); //12
            writer.Write((int)version); //16
            writer.Write(compressedLength); //20
            writer.Write(uncompressedLength); //24
            writer.Write(eventId.ToByteArray()); //40
            writer.Write(streamName); // 40+13=53
            writer.Write(eventType); // 53+10=61
            writer.Write(compressed, 0, compressedLength); // now 63
            writer.Flush();

            var result = new byte[target.Length];
            target.Seek(0, SeekOrigin.Begin);
            target.Read(result, 0, result.Length);

            //Console.WriteLine($"length: {length} vs. {result.Length}");
            return result;
        }

        private byte[] BuildPayload(byte[] metadata, byte[] data)
        {
            var merged = new MemoryStream();
            var merged_writer = new BinaryWriter(merged);

            merged_writer.Write((Int32)metadata.Length);
            merged_writer.Write((Int32)data.Length);
            merged_writer.Write(metadata);
            merged_writer.Write(data);
            merged_writer.Flush();
            merged_writer.Seek(0, SeekOrigin.Begin);
            var result = new byte[merged.Length];
            merged.Read(result, 0, result.Length);

            return result;
        }
    }
}