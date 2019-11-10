using System;
using System.Collections.Generic;
using System.Data.HashFunction.xxHash;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ESPlus.Exceptions;
using ESPlus.Extentions;
using ESPlus.Storage;
using ESPlus.Subscribers;
using LZ4;

namespace ESPlus.Wyrm
{
    public enum Commands
    {
        Protocol = 0,
        Ping = 1,
        Close = 3,
        Put = 4,
        AuthenticateJwt = 5,
        AuthenticateApiKey = 6,
        EventFilter = 7,
        ReadAllForward = 8,
        ReadAllBackward = 9,
        ReadStreamForward = 10,
        ReadStreamBackward = 11,
        SubscribeAll = 12,
        SuscribeStream = 13,
        Pull = 14,
        SubscribePull = 15,
        Exception = 16,
    }

    public enum Queries
    {
        Ahead = 0,
        PullCreated = 1,
        PullDeleted = 2,
        PullEvent = 3,
        Event = 4,
        Deleted = 5,
        Success = 6,
        Pong = 7,
        Exception = 8,
    }
    
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

        private TcpClient Create()
        {
            var client = new TcpClient();
            client.NoDelay = false;
            
            Retry(() => client.Connect(_host, _port));

            return client;
        }
        
        private void Retry(Action action)
        {
            Exception exception = null;
            
            for (var tries = 0; tries < 3; ++tries)
            {
                try
                {
                    action();
                    return;
                }
                catch (Exception ex)
                {
                    exception = ex;
                    Thread.Sleep(TimeSpan.FromSeconds(1 << tries));
                }
            }

            throw exception;
        }
        
        public IEnumerable<WyrmEvent2> EnumerateStream(string streamName)
        {
            using (var client = Create())
            using (var stream = client.GetStream())
            using (var reader = new BinaryReader(stream))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write((int)12+streamName.Length);
                writer.Write((int)10);
                writer.Write((int)streamName.Length);
                writer.Write(Encoding.UTF8.GetBytes(streamName));
                writer.Flush();

                while (true)
                {
                    var length = reader.ReadInt32(); 
                    var query = (Queries) reader.ReadInt32();
                    var tokenizer = new Tokenizer(reader.ReadBytes(length - sizeof(Int32) * 2));

                    if (query == Queries.Success)
                    {
                        yield break;
                    }
                    else if (query == Queries.Event)
                    {
                        yield return ReadEvent(tokenizer);
                    }
                    else if (query == Queries.Exception)
                    {
                        
                    }
                }
            }
        }

        private WyrmEvent2 ReadEvent(Tokenizer tokenizer)
        {
            var version = tokenizer.ReadI64();
            var time = tokenizer.ReadDateTime();
            var offset = tokenizer.ReadI64();
            var totalOffset = tokenizer.ReadI64();
            var position = tokenizer.ReadBinary(32);
            var eventId = tokenizer.ReadGuid();
            var eventType = tokenizer.ReadString();
            var streamName = tokenizer.ReadString();
            var metadataLength = tokenizer.ReadI32();
            var dataLength = tokenizer.ReadI32();
            var metadata = tokenizer.ReadBinary(metadataLength);
            var data = tokenizer.ReadBinary(dataLength);
                        
            return new WyrmEvent2
            {
                EventType = eventType,
                Offset = offset,
                TotalOffset = totalOffset,
                Version = version,
                Position = position,
                StreamName = streamName,
                EventId = eventId,
                Serializer = Serializer,
                Timestamp = time,
                Metadata = metadata,
                Data = data,
            };
        }

        public async Task DeleteAsync(string streamName, long version)
        {
throw new NotImplementedException();
        }

        public Task<Position> Append(Bundle bundle)
        {
            var position = Position.Start;
            
            using (var client = Create())
            using (var stream = client.GetStream())
            using (var reader = new BinaryReader(stream))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write((int)4+4+4+bundle.Items.Sum(x => x.Count()));
                writer.Write((int)OperationType.PUT);
                writer.Write((int)CommitPolicy.All);

                foreach (var item in bundle.Items)
                {
                    if (item is CreateBundleItem createItem)
                    {
                        writer.Write((int)BundleOp.Create);
                        writer.Write((int)createItem.StreamName.Length);
                        writer.Write(Encoding.UTF8.GetBytes(createItem.StreamName));
                    }
                    else if (item is DeleteBundleItem deleteItem)
                    {
                        writer.Write((int)BundleOp.Delete);
                        writer.Write((long)deleteItem.StreamVersion);
                        writer.Write((int)deleteItem.StreamName.Length);
                        writer.Write(Encoding.UTF8.GetBytes(deleteItem.StreamName));
                    }
                    else if (item is EventsBundleItem eventsItem)
                    {
                        writer.Write((int)BundleOp.Events);
                        writer.Write((long)eventsItem.StreamVersion);
                        writer.Write((int)eventsItem.StreamName.Length);
                        writer.Write(Encoding.UTF8.GetBytes(eventsItem.StreamName));
                        writer.Write((int)eventsItem.Events.Count());

                        foreach (var evt in eventsItem.Events)
                        {
                            writer.WriteStruct(evt.EventId);
                            writer.Write((int) evt.EventType.Length);
                            writer.Write(Encoding.UTF8.GetBytes(evt.EventType));
                            writer.Write((int) evt.Metadata.Length);
                            writer.Write(evt.Metadata);
                            writer.Write((int) evt.Body.Length);
                            writer.Write(evt.Body);
                        }
                    }
                }
                
                while (true)
                {
                    var length = reader.ReadInt32(); 
                    var query = (Queries) reader.ReadInt32();
                    var tokenizer = new Tokenizer(reader.ReadBytes(length - sizeof(Int32) * 2));

                    if (query == Queries.Success)
                    {
                        break;
                    }
                    else if (query == Queries.Exception)
                    {
                        ReadException(tokenizer);
                    }
                }
                
                client.Close();
            }
            
            return Task.FromResult(position);
        }

        private void ReadException(Tokenizer tokenizer)
        {
            var code = tokenizer.ReadI32();
            var message = tokenizer.ReadString();
            
            throw new Exception(message);
        }

        public Task<Position> Append(IEnumerable<WyrmEvent> events)
        {
            return Task.FromResult(Position.Start);
        }

        public IEnumerable<string> EnumerateStreams(params Type[] filters)
        {
            var algorithm = xxHashFactory.Instance.Create(new xxHashConfig() { HashSizeInBits = 64 });

            using (var client = Create())
            using (var stream = client.GetStream())
            using (var reader = new BinaryReader(stream))
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
                    var length = reader.ReadInt32();

                    if (length == 0)
                    {
                        yield break;
                    }

                    var buffer = reader.ReadBytes(length);

                    yield return Encoding.UTF8.GetString(buffer);
                }
            }
        }
        
        public Position LastCheckpoint()
        {
            Console.WriteLine($"LastCheckpoint");
            using (var client = Create())
            using (var stream = client.GetStream())
            using (var reader = new BinaryReader(stream))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(OperationType.LAST_CHECKPOINT);
                writer.Flush();

                while (true)
                {
                    var position = reader.ReadBytes(32);

                    return new Position(position);
                }
            }
        }

        public IEnumerable<WyrmEvent2> Subscribe(Position from)
        {
//            Console.WriteLine($"Subscribe: {from.AsHexString()}");
//            using (var client = Create())
//            using (var stream = client.GetStream())
//            using (var reader = new BinaryReader(stream))
//            using (var writer = new BinaryWriter(stream))
//            {
//                writer.Write(OperationType.SUBSCRIBE);
//                writer.Write(@from.Binary);
//                writer.Flush();
//
//                while (true)
//                {
//                    var length = reader.ReadInt32();
//
//                    if (length == 8)
//                    {
//                        //Console.WriteLine("reached end!");
//                        break;
//                    }
//
//                    yield return ReadEvent(reader, length - sizeof(Int32));
//                }
//            }
            throw new NotImplementedException();
        }

        public IEnumerable<WyrmEvent2> EnumerateAll(Position from)
        {
            throw new NotImplementedException();
        }        

        public IEnumerable<WyrmEvent2> EnumerateAllByStreams(params Type[] filters)
        {
            throw new NotImplementedException();
//            var algorithm = xxHashFactory.Instance.Create(new xxHashConfig() { HashSizeInBits = 64 });
//
//            using (var client = Create())
//            using (var stream = client.GetStream())
//            using (var reader = new BinaryReader(stream))
//            using (var writer = new BinaryWriter(stream))
//            {
//                writer.Write(OperationType.READ_ALL_STREAMS_FORWARD);
//                writer.Write(filters.Length);
//                foreach (var filter in filters)
//                {
//                    writer.Write(BitConverter.ToInt64(algorithm.ComputeHash(Encoding.UTF8.GetBytes(filter.FullName)).Hash));
//                }
//
//                writer.Flush();
//
//                while (true)
//                {
//                    var length = reader.ReadInt32();
//
//                    if (length == 8)
//                    {
//                        //Console.WriteLine("reached end!");
//                        break;
//                    }
//
//                    yield return ReadEvent(reader, length - sizeof(Int32));
//                }
//            }
        }

        public void InvokeException()
        {
            using (var client = Create())
            using (var stream = client.GetStream())
            using (var reader = new BinaryReader(stream))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write((int) 8);
                writer.Write((int) Commands.Exception);
                writer.Flush();

                while (true)
                {
                    var length = reader.ReadInt32(); 
                    var query = (Queries) reader.ReadInt32();
                    var tokenizer = new Tokenizer(reader.ReadBytes(length - sizeof(Int32) * 2));

                    if (query == Queries.Success)
                    {
                        return;
                    }
                    else if (query == Queries.Exception)
                    {
                        ReadException(tokenizer);
                    }
                }                
            }
        }
    }
}
