using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using EventStore.ClientAPI;

namespace ESPlus.Wyrm
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
        public string EventType;
        public string StreamName;
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
            public Position Position;
            public Int64 Offset;
            public Int64 TotalOffset;
            public Guid EventId;
            public Int64 Version;
            public Int32 UncompressedSize;
            public Int32 CompressedSize;
            public Int32 EncryptedSize;
            public Int64 Clock;
            public Int64 Ms;
            public Int32 EventTypeLength;
            public Int32 StreamNameLength;
            public Int32 MetaDataLength;
            public Int32 PayloadLength;
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
                var length = stream.ReadStructAsync<Int32>().Result;

                if (length == 8)
                {
                    //Console.WriteLine("reached end!");
                    break;
                }

                var monkey = stream.ReadStructAsync<Monkey>().Result;
                var epooch = new DateTime(1970, 1, 1);
                var time = epooch.AddSeconds(monkey.Clock).AddMilliseconds(monkey.Ms).ToLocalTime();
                var metadata = new byte[0];
                var data = new byte[0];
                var streamName2 = Encoding.UTF8.GetString(stream.ReadBytesAsync(monkey.StreamNameLength).Result);
                var eventType = Encoding.UTF8.GetString(stream.ReadBytesAsync(monkey.EventTypeLength).Result);
                var compressed = stream.ReadBytesAsync((int)monkey.CompressedSize).Result;
                var uncompressed = new byte[monkey.UncompressedSize];
                var result = LZ4.LZ4_decompress_safe(compressed, uncompressed, compressed.Length, uncompressed.Length);

                using (var mx = new MemoryStream(uncompressed))
                {
                    mx.Seek(0, SeekOrigin.Begin);
                    using (var ms2 = new BinaryReader(mx))
                    {
                        metadata = ms2.ReadBytes(monkey.MetaDataLength);
                        data = ms2.ReadBytes(monkey.PayloadLength);
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
                    EventType = eventType,
                    StreamName = streamName2
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

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct Apa
        {
            public Int32 Length;
            public Int32 StreamNameLength;
            public Int32 EventTypeLength;
            public Int64 Version;
            public Int32 CompressedLength;
            public Int32 UncompressedLength;
            public Guid EventId;
            public Int32 MetaDataLength;
            public Int32 BodyLength;
        }

        private byte[] Assemble(WyrmEvent @event)
        {
            var target = new MemoryStream();
            var writer = new BinaryWriter(target);
            var streamName = Encoding.UTF8.GetBytes(@event.StreamName);
            var eventType = Encoding.UTF8.GetBytes(@event.EventType);
            var metadata = @event.Metadata;
            var body = @event.Body;
            var uncompressed = BuildPayload(metadata, body);
            var uncompressedLength = uncompressed.Length;
            var compressed = new byte[LZ4.LZ4_compressBound(uncompressedLength)];
            var compressedLength = LZ4.LZ4_compress_default(uncompressed, compressed, uncompressedLength, compressed.Length);
            var length = compressedLength + streamName.Length + eventType.Length + Marshal.SizeOf(typeof(Apa));
            var apa = new Apa
            {
                Length = length,
                StreamNameLength = streamName.Length,
                EventTypeLength = eventType.Length,
                Version = @event.Version,
                CompressedLength = compressedLength,
                UncompressedLength = uncompressedLength,
                EventId = @event.EventId,
                MetaDataLength = metadata.Length,
                BodyLength = body.Length
            };

            writer.WriteStruct(apa);
            writer.Write(streamName);
            writer.Write(eventType);
            writer.Write(compressed, 0, compressedLength);
            writer.Flush();

            var result = new byte[target.Length];
            target.Seek(0, SeekOrigin.Begin);
            target.Read(result, 0, result.Length);
            return result;
        }

        private byte[] BuildPayload(byte[] metadata, byte[] data)
        {
            return metadata.Concat(data).ToArray();
        }
    }
}