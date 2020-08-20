using System;
using System.Collections.Generic;
using System.Data.HashFunction.xxHash;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ESPlus.Exceptions;
using ESPlus.Extentions;
using LZ4;

namespace ESPlus.Wyrm
{
    public class WyrmDriver : IWyrmDriver
    {
        private readonly string _host;
        private readonly int _port;
        public IEventSerializer Serializer { get; }

        public WyrmDriver(string connectionString, IEventSerializer eventSerializer)
        {
            var parts = connectionString.Split(":");

            _host = parts[0];
            _port = int.Parse(parts[1]);
            Serializer = eventSerializer;
        }

        private byte[] Combine(params byte[][] arrays)
        {
            var rv = new byte[arrays.Sum(a => a.Length)];
            var offset = 0;

            foreach (var array in arrays)
            {
                Buffer.BlockCopy(array, 0, rv, offset, array.Length);
                offset += array.Length;
            }

            return rv;
        }

        private async Task<TcpClient> CreateAsync()
        {
            var client = new TcpClient();
            client.NoDelay = false;
            
            await Retry.RetryAsync(() => client.ConnectAsync(_host, _port));
            return client;
        }
        
        public IEnumerable<WyrmEvent2> EnumerateStream(string streamName)
        {
            using (var client = CreateAsync().Result)
            using (var stream = client.GetStream())
            using (var reader = new BinaryReader(stream))
            using (var writer = new BinaryWriter(stream))
            {
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

                    var evt = ReadEvent(reader, length - sizeof(Int32));

                    // Console.WriteLine($"Stream: {evt.StreamName}.({evt.Version}){evt.EventType}");

                    yield return evt;
                }
            }
        }

        private WyrmEvent2 ReadEvent(BinaryReader reader, int length)
        {
            ReadOnlySpan<byte> payload = reader.ReadBytes(length);// stackalloc byte[length];
            var createEvent = "";
            // reader.ReadBytes(payload, length);

            int disp = 0;
            var position = payload.Slice(disp, 32).ToArray();
            disp += 32;
            var offset = BitConverter.ToInt64(payload.Slice(disp, 8));
            disp += 8;
            var totalOffset = BitConverter.ToInt64(payload.Slice(disp, 8));
            disp += 8;
            var ahead = BitConverter.ToBoolean(payload.Slice(disp, 1));
            disp += 1;
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
            var time = DateTimeOffset.FromUnixTimeSeconds(clock).AddTicks(ms * 10);
            var compressed = payload.Slice(disp, (int)compressedSize).ToArray();
            disp += compressedSize;
            var uncompressed = new byte[uncompressedSize];
            var compressedLength = LZ4Codec.Decode(compressed, 0, compressed.Length, uncompressed, 0, uncompressed.Length);
            var metadata = new byte[metaDataLength];
            var data = new byte[payloadLength];
            
            if (eventType == "Wyrm.StreamDeleted" && disp < payload.Length)
            {
                var createEventLength = BitConverter.ToInt32(payload.Slice(disp, 4));
                disp += 4;
                createEvent = Encoding.UTF8.GetString(payload.Slice(disp, createEventLength));
            }
            
            Array.Copy(uncompressed, metadata, metadata.Length);
            Array.Copy(uncompressed, metadata.Length, data, 0, data.Length);

            return new WyrmEvent2
            {
                Offset = offset,
                TotalOffset = totalOffset,
                EventId = eventId,
                Version = version,
                TimestampUtc = time.DateTime,
                Metadata = metadata,
                Data = data,
                EventType = eventType,
                StreamName = streamName,
                Position = position,
                Serializer = Serializer,
                IsAhead = ahead,
                CreateEvent = createEvent,
            };
        }

        private async Task<WyrmEvent2> ReadEventAsync(NetworkStream reader, int length,
            CancellationToken cancellationToken)
        {
            ReadOnlyMemory<byte> payload = await reader.ReadBinaryAsync(length, cancellationToken);
            var createEvent = "";

            int disp = 0;
            var position = payload.Slice(disp, 32).ToArray();
            disp += 32;
            var offset = BitConverter.ToInt64(payload.Slice(disp, 8).ToArray());
            disp += 8;
            var totalOffset = BitConverter.ToInt64(payload.Slice(disp, 8).ToArray());
            disp += 8;
            var ahead = BitConverter.ToBoolean(payload.Slice(disp, 1).ToArray());
            disp += 1;
            var eventId = new Guid(payload.Slice(disp, 16).ToArray());
            disp += 16;
            var version = BitConverter.ToInt64(payload.Slice(disp, 8).ToArray());
            disp += 8;
            var uncompressedSize = BitConverter.ToInt32(payload.Slice(disp, 4).ToArray());
            disp += 4;
            var compressedSize = BitConverter.ToInt32(payload.Slice(disp, 4).ToArray());
            disp += 4;
            var encryptedSize = BitConverter.ToInt32(payload.Slice(disp, 4).ToArray());
            disp += 4;
            var clock = BitConverter.ToInt64(payload.Slice(disp, 8).ToArray());
            disp += 8;
            var ms = BitConverter.ToInt64(payload.Slice(disp, 8).ToArray());
            disp += 8;
            var eventTypeLength = BitConverter.ToInt32(payload.Slice(disp, 4).ToArray());
            disp += 4;
            var streamNameLength = BitConverter.ToInt32(payload.Slice(disp, 4).ToArray());
            disp += 4;
            var metaDataLength = BitConverter.ToInt32(payload.Slice(disp, 4).ToArray());
            disp += 4;
            var payloadLength = BitConverter.ToInt32(payload.Slice(disp, 4).ToArray());
            disp += 4;
            var streamName = Encoding.UTF8.GetString(payload.Slice(disp, (int) streamNameLength).ToArray());
            disp += (int) streamNameLength;
            var eventType = Encoding.UTF8.GetString(payload.Slice(disp, (int) eventTypeLength).ToArray());
            disp += (int) eventTypeLength;
            var time = DateTimeOffset.FromUnixTimeSeconds(clock).AddTicks(ms * 10);
            var compressed = payload.Slice(disp, (int) compressedSize).ToArray();
            disp += compressedSize;
            var uncompressed = new byte[uncompressedSize];
            var compressedLength =
                LZ4Codec.Decode(compressed, 0, compressed.Length, uncompressed, 0, uncompressed.Length);
            var metadata = new byte[metaDataLength];
            var data = new byte[payloadLength];

            if (eventType == "Wyrm.StreamDeleted" && disp < payload.Length)
            {
                var createEventLength = BitConverter.ToInt32(payload.Slice(disp, 4).ToArray());
                disp += 4;
                createEvent = Encoding.UTF8.GetString(payload.Slice(disp, createEventLength).ToArray());
            }

            Array.Copy(uncompressed, metadata, metadata.Length);
            Array.Copy(uncompressed, metadata.Length, data, 0, data.Length);

            return new WyrmEvent2
            {
                Offset = offset,
                TotalOffset = totalOffset,
                EventId = eventId,
                Version = version,
                TimestampUtc = time.DateTime,
                Metadata = metadata,
                Data = data,
                EventType = eventType,
                StreamName = streamName,
                Position = position,
                Serializer = Serializer,
                IsAhead = ahead,
                CreateEvent = createEvent,
            };
        }

        public Task PingAsync()
        {
            return Task.CompletedTask;
        }        

        public async Task DeleteAsync(string streamName, long version, CancellationToken cancellationToken)
        {
            using (var client = await CreateAsync())
            using (var stream = client.GetStream())
            using (var reader = new BinaryReader(stream))
            using (var writer = new BinaryWriter(stream))
            {
                var name = Encoding.UTF8.GetBytes(streamName);
                writer.Write(OperationType.DELETE);
                writer.Write(name.Length);
                writer.Write(name, 0, name.Length);

                var len = await stream.ReadInt32Async(cancellationToken);

                if (len != 8)
                {
                    throw new Exception("if (len != 8)");
                }

                // var status = reader.ReadInt32();
                // if (status != 0)
                // {
                //     throw new Exception($"if (status != 0): {status}");
                // }
            }
        }

        public Task<WyrmResult> Append(IEnumerable<WyrmEvent> events)
        {
            if (!events.Any())
            {
                return Task.FromResult(new WyrmResult(Position.Start, 0L));
            }

            using (var client = CreateAsync().Result)
            using (var stream = client.GetStream())
            using (var reader = new BinaryReader(stream))
            using (var writer = new BinaryWriter(stream))
            {
                var concat = Combine(events.Select(x => Assemble(x)).ToArray());
                int length = concat.Length;

                writer.Write(OperationType.PUT);
                writer.Write(length);
                writer.Write(concat, 0, length);
                writer.Flush();

                var len = reader.ReadInt32();

                if (len == 8 + 32)
                {
                    var status = reader.ReadInt32();
                    var hash = reader.ReadBytes(32);
                    var offset = 0;

                    if (status != 0)
                    {
                        throw new WrongExpectedVersionException($"Bad status: {status}");
                    }

                    return Task.FromResult(new WyrmResult(new Position(hash), offset));
                }
                else if (len == 8 + 32 + 8)
                {
                    var status = reader.ReadInt32();
                    var hash = reader.ReadBytes(32);
                    var offset = reader.ReadInt64();

                    if (status != 0)
                    {
                        throw new WrongExpectedVersionException($"Bad status: {status}");
                    }

                    return Task.FromResult(new WyrmResult(new Position(hash), offset));
                }
                else
                {
                    throw new Exception("if (len != 8 + 32 + 8?)");
                }
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

        public async IAsyncEnumerable<string> EnumerateStreams(CancellationToken cancellationToken, params Type[] filters)
        {
            var algorithm = xxHashFactory.Instance.Create(new xxHashConfig() { HashSizeInBits = 64 });

            using (var client = CreateAsync().Result)
            using (var stream = client.GetStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(OperationType.LIST_STREAMS);
                writer.Write(filters.Length);
                foreach (var filter in filters)
                {
                    writer.Write(BitConverter.ToInt64(algorithm.ComputeHash(Encoding.UTF8.GetBytes(filter.FullName)).Hash));
                }
                writer.Flush();

                while (true)
                {
                    var length = await stream.ReadInt32Async(cancellationToken);

                    if (length == 0)
                    {
                        yield break;
                    }

                    var buffer = await stream.ReadBinaryAsync(length, cancellationToken);

                    yield return Encoding.UTF8.GetString(buffer);
                }
            }
        }
        
        public async Task<Position> LastCheckpointAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine($"LastCheckpoint");
            using (var client = CreateAsync().Result)
            using (var stream = client.GetStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(OperationType.LAST_CHECKPOINT);
                writer.Flush();

                while (true)
                {
                    var position = await stream.ReadBinaryAsync(32, cancellationToken);

                    return new Position(position);
                }
            }
        }

        public IEnumerable<WyrmEvent2> Subscribe(Position from)
        {
            Console.WriteLine($"Subscribe: {from.AsHexString()}");
            using (var client = CreateAsync().Result)
            using (var stream = client.GetStream())
            using (var reader = new BinaryReader(stream))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(OperationType.SUBSCRIBE);
                writer.Write(@from.Binary);
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
        
        public async IAsyncEnumerable<WyrmEvent2> SubscribeAsync(Position from, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            Console.WriteLine($"Subscribe: {from.AsHexString()}");
            using (var client = await CreateAsync())
            using (var stream = client.GetStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(OperationType.SUBSCRIBE);
                writer.Write(from.Binary);
                writer.Flush();

                while (!cancellationToken.IsCancellationRequested)
                {
                    var length = await stream.ReadInt32Async(cancellationToken);

                    if (length == 8)
                    {
                        //Console.WriteLine("reached end!");
                        break;
                    }

                    yield return await ReadEventAsync(stream, length - sizeof(Int32), cancellationToken);
                }
            }
        }

        public IEnumerable<WyrmEvent2> EnumerateAll(Position from)
        {
            Console.WriteLine($"EnumerateAll: {from.AsHexString()}");
            using (var client = CreateAsync().Result)
            using (var stream = client.GetStream())
            using (var reader = new BinaryReader(stream))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(OperationType.READ_ALL_FORWARD);
                writer.Write(@from.Binary);
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

        public async IAsyncEnumerable<WyrmEvent2> EnumerateAllByStreamsAsync(CancellationToken cancellationToken,
            params Type[] filters)
        {
            var algorithm = xxHashFactory.Instance.Create(new xxHashConfig { HashSizeInBits = 64 });

            using (var client = CreateAsync().Result)
            using (var stream = client.GetStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(OperationType.READ_ALL_STREAMS_FORWARD);
                writer.Write(filters.Length);
                foreach (var filter in filters)
                {
                    writer.Write(BitConverter.ToInt64(algorithm.ComputeHash(Encoding.UTF8.GetBytes(filter.FullName)).Hash));
                }

                writer.Flush();

                while (true)
                {
                    var length = await stream.ReadInt32Async(cancellationToken);

                    if (length == 8)
                    {
                        //Console.WriteLine("reached end!");
                        break;
                    }

                    yield return await ReadEventAsync(stream, length - sizeof(Int32), cancellationToken);
                }
            }
        }

        private byte[] Assemble(WyrmEvent @event)
        {
            using (var target = new MemoryStream())
            using (var writer = new BinaryWriter(target))
            {
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
        }

        private byte[] BuildPayload(byte[] metadata, byte[] data)
        {
            return metadata.Concat(data).ToArray();
        }
    }
}
