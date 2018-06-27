using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
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

        public IEnumerable<WyrmEvent2> EnumerateStream(string streamName)
        {
            var client = new TcpClient();

            client.ConnectAsync("localhost", 8888).Wait();

            var reader = new BinaryReader(client.GetStream());
            var writer = new BinaryWriter(client.GetStream());
            var name = Encoding.UTF8.GetBytes(streamName);
            writer.Write(OperationType.READ_STREAM_FORWARD);
            writer.Write(name.Length);
            writer.Write(name, 0, name.Length);
            writer.Write((int)0); //filter
            writer.Flush();

            while (true)
            {
                var length = reader.ReadInt64();

                if (length == 8)
                {
                    Console.WriteLine("reached end!");
                    break;
                }

                // Console.WriteLine($"length: {length}");
                var eventTypeHash = reader.ReadUInt64();
                var position = reader.ReadBytes(32);
                var offset = reader.ReadInt64();
                var totalOffset = reader.ReadInt64();
                var eventId = new Guid(reader.ReadBytes(16));
                var version = reader.ReadInt64();
                var uncompressedSize = reader.ReadInt64();
                var compressedSize = reader.ReadInt64();
                var encryptedSize = reader.ReadInt64();
                var clock = reader.ReadInt64();
                var ms = reader.ReadInt64();
                var epooch = new DateTime(1970, 1, 1);
                var time = epooch.AddSeconds(clock).AddMilliseconds(ms).ToLocalTime();
                //var LengthOfMetadata = reader.ReadInt32();

                // //Console.WriteLine($"position: {position}");
                //Console.WriteLine($"offset: {offset}");
                //Console.WriteLine($"totalOffset: {totalOffset}");
                // Console.WriteLine($"eventId: {eventId}");
                // Console.WriteLine($"version: {version}");
                // Console.WriteLine($"uncompressedSize: {uncompressedSize}");
                // Console.WriteLine($"compressedSize: {compressedSize}");
                // Console.WriteLine($"encryptedSize: {encryptedSize}");
                // Console.WriteLine($"time: {time:yyyy-MM-dd HH:mm:ss.ffffff}");
                var metadata = new byte[0];
                var data = new byte[0];
                var compressed = reader.ReadBytes((int)compressedSize);
                var uncompressed = new byte[uncompressedSize];
                var result = LZ4.LZ4_decompress_safe(compressed, uncompressed, compressed.Length, uncompressed.Length);

                // for (int i = 0; i < compressedSize; ++i)
                // {
                //     var c = compressed[i];
                //     Console.WriteLine("uncompress: " + (int)c + " " + (char)c);
                // }

                //int LZ4_decompress_safe (const char* source, char* dest, int compressedSize, int maxOutputSize);
                using (var mx = new MemoryStream(uncompressed))
                {
                    mx.Seek(0, SeekOrigin.Begin);
                    using (var ms2 = new BinaryReader(mx))
                    {
                        var lengthOfMetadata = ms2.ReadInt32();
                        var lengthOfData = ms2.ReadInt32();
                        // Console.WriteLine($"lengthOfMetadata: {lengthOfMetadata}");
                        // Console.WriteLine($"lengthOfData: {lengthOfData}");
                        // Console.WriteLine($"uncompressed: {uncompressed.Length} vs. {result}");
                        metadata = ms2.ReadBytes(lengthOfMetadata);
                        data = ms2.ReadBytes(lengthOfData);
                    }
                }

                // Console.WriteLine($"Metadata: [{Encoding.UTF8.GetString(metadata)}]");
                // Console.WriteLine($"Data: [{Encoding.UTF8.GetString(data)}]");

                yield return new WyrmEvent2
                {
                    Offset = offset,
                    TotalOffset = totalOffset,
                    EventId = eventId,
                    Version = version,
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