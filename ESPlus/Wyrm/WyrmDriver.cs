using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using LZ4;

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
        public string EventType { get; set; }
        public string StreamName { get; set; }
        public byte[] Position { get; set; }
        public IEventSerializer Serializer { get; set; }
    }

    public class WyrmDriver
    {
        private string _host;
        private int _port;
        public IEventSerializer Serializer { get; }

        // public WyrmDriver()
        //     : this(Environment.GetEnvironmentVariable("EVENTSTORE") ?? "localhost:8888")
        // {
        // }

        public WyrmDriver(string connectionString, IEventSerializer eventSerializer)
        {
            var parts = connectionString.Split(":");

            _host = parts[0];
            _port = int.Parse(parts[1]);
            Serializer = eventSerializer;
        }

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

        private TcpClient Create()
        {
            var client = new TcpClient();
            client.NoDelay = false;
            client.ConnectAsync(_host, _port).Wait();

            return client;
        }

        public IEnumerable<WyrmEvent2> EnumerateStream(string streamName)
        {
            using (var client = Create())
            {
                var stream = client.GetStream();
                var writer = new BinaryWriter(client.GetStream());
                var reader = new BinaryReader(client.GetStream());
                var name = Encoding.UTF8.GetBytes(streamName);
                writer.Write(OperationType.READ_STREAM_FORWARD);
                writer.Write(name.Length);
                writer.Write(name, 0, name.Length);
                writer.Write((int)0); //filter
                writer.Flush();

                while (true)
                {
                    var length = reader.ReadInt32();

                    if (length == 8)
                    {
                        //Console.WriteLine("reached end!");
                        break;
                    }

                    yield return ReadEvent(reader, length - sizeof(Int32));
                }
            }
        }

        public WyrmEvent2 ReadEvent(BinaryReader reader, int length)
        {
            ReadOnlySpan<byte> payload = reader.ReadBytes(length);// stackalloc byte[length];

            // reader.ReadBytes(payload, length);

            int disp = 0;
            var position = payload.Slice(disp, 32).ToArray();
            disp += 32;
            var offset = BitConverter.ToInt64(payload.Slice(disp, 8));
            disp += 8;
            var totalOffset = BitConverter.ToInt64(payload.Slice(disp, 8));
            disp += 8;
            var eventId = new Guid(payload.Slice(disp, 16));
            disp += 16;
            var version = BitConverter.ToInt64(payload.Slice(disp, 8));
            disp += 8;
            var uncompressedSize = BitConverter.ToInt32(payload.Slice(disp, 4));
            disp += 4;
            var compressedSize = BitConverter.ToInt32(payload.Slice(disp, 4));
            disp += 4;
            var encryptedSize = BitConverter.ToInt32(payload.Slice(disp, 4));
            disp += 4;
            var clock = BitConverter.ToInt64(payload.Slice(disp, 8));
            disp += 8;
            var ms = BitConverter.ToInt64(payload.Slice(disp, 8));
            disp += 8;
            var eventTypeLength = BitConverter.ToInt32(payload.Slice(disp, 4));
            disp += 4;
            var streamNameLength = BitConverter.ToInt32(payload.Slice(disp, 4));
            disp += 4;
            var metaDataLength = BitConverter.ToInt32(payload.Slice(disp, 4));
            disp += 4;
            var payloadLength = BitConverter.ToInt32(payload.Slice(disp, 4));
            disp += 4;
            var streamName = Encoding.UTF8.GetString(payload.Slice(disp, (int)streamNameLength));
            disp += (int)streamNameLength;
            var eventType = Encoding.UTF8.GetString(payload.Slice(disp, (int)eventTypeLength));
            disp += (int)eventTypeLength;
            var epooch = new DateTime(1970, 1, 1);
            var time = epooch.AddSeconds(clock).AddMilliseconds(ms).ToLocalTime();
            var compressed = payload.Slice(disp, (int)compressedSize).ToArray();
            var uncompressed = new byte[uncompressedSize];
            var compressedLength = LZ4Codec.Decode(compressed, 0, compressed.Length, uncompressed, 0, uncompressed.Length);
            var metadata = new byte[metaDataLength];
            var data = new byte[payloadLength];

            Array.Copy(uncompressed, metadata, metadata.Length);
            Array.Copy(uncompressed, metadata.Length, data, 0, data.Length);

            return new WyrmEvent2
            {
                Offset = offset,
                TotalOffset = totalOffset,
                EventId = eventId,
                Version = version,
                Timestamp = time,
                Metadata = metadata,
                Data = data,
                EventType = eventType,
                StreamName = streamName,
                Position = position,
                Serializer = Serializer
            };
        }

        public async Task DeleteAsync(string streamName)
        {
            using (var client = Create())
            {
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
        }

        public async Task Append(IEnumerable<WyrmEvent> events)
        {
            if (!events.Any())
            {
                await Task.FromResult(0);
            }

            using (var client = Create())
            {
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
                    throw new WrongExpectedVersionException($"Bad status: {status}");
                }
                await Task.FromResult(0);
            }
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

        public IEnumerable<string> EnumerateStreams()
        {
            using (var client = Create())
            {
                var stream = client.GetStream();
                var writer = new BinaryWriter(stream);
                var reader = new BinaryReader(stream);

                writer.Write(OperationType.LIST_STREAMS);
                writer.Flush();

                while (true)
                {
                    var length = reader.ReadInt32();

                    if (length == 0)
                    {
                        yield break;
                    }

                    var buffer = stream.ReadBytes(length);

                    yield return Encoding.UTF8.GetString(buffer);
                }
            }
        }

        public IEnumerable<WyrmEvent2> EnumerateAll(byte[] from)
        {
            Console.WriteLine($"EnumerateAll: {from.AsHexString()}");
            using (var client = Create())
            {
                var stream = client.GetStream();
                var writer = new BinaryWriter(stream);
                var reader = new BinaryReader(stream);

                writer.Write(OperationType.SUBSCRIBE);
                writer.Write(from);
                writer.Flush();

                while (true)
                {
                    var length = reader.ReadInt32();

                    if (length == 8)
                    {
                        //Console.WriteLine("reached end!");
                        break;
                    }

                    yield return ReadEvent(reader, length - sizeof(Int32));
                }
            }
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
            var compressed = new byte[LZ4Codec.MaximumOutputLength(uncompressedLength)];
            var compressedLength = LZ4Codec.Encode(uncompressed, 0, uncompressedLength, compressed, 0, compressed.Length);

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
