using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using EventStore.ClientAPI;

namespace ESPlus
{
    public class LZ4
    {
        [DllImport("liblz4")]
        public static extern int LZ4_compress_default(byte[] source, byte[] dest, int sourceSize, int maxDestSize);

        [DllImport("liblz4")]
        public static extern int LZ4_compressBound(int inputSize);
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

            if (status != 200)
            {
                throw new Exception("if (status != 200)");
            }
            await Task.FromResult(0);
        }

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
            var length = compressedLength + 4 + 4 + 4 + 4 + 4 + 20 + streamName.Length + eventType.Length;

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