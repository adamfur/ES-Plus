using System;
using System.Collections.Generic;
using System.Data.HashFunction.xxHash;
using System.Diagnostics;
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
using MongoDB.Driver;

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
        ListStreams = 17,
        ReadAllForwardGroupByStream = 18,
        Checkpoint = 19,
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
        Checkpoint = 9,
        StreamVersion = 10,
        StreamName = 11,
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

        private IEnumerable<WyrmItem> ReadStream(string streamName, Commands command)
        {
            using (var client = Create())
            using (var stream = client.GetStream())
            using (var reader = new BinaryReader(stream))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write((int) 12 + streamName.Length);
                writer.Write((int) command);
                writer.Write((int) streamName.Length);
                writer.Write(Encoding.UTF8.GetBytes(streamName));
                writer.Flush();

                while (true)
                {
                    var length = reader.ReadInt32();
                    var query = (Queries) reader.ReadInt32();
                    var payload = reader.ReadBytes(length - sizeof(Int32) * 2);
                    var tokenizer = new Tokenizer(payload);

                    if (query == Queries.Success)
                    {
                        yield break;
                    }
                    else if (query == Queries.Event)
                    {
                        yield return ParseEvent(tokenizer);
                    }
                    else if (query == Queries.StreamVersion)
                    {
                        yield return ParseStreamVersion(tokenizer);
                    }
                    else if (query == Queries.Exception)
                    {
                        ParseException(tokenizer);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
            }
        }

        public IEnumerable<WyrmItem> ReadAllForward(Position position)
        {
            return ReadAll(position, Commands.ReadAllForward);
        }

        public IEnumerable<WyrmItem> ReadAllBackward(Position position)
        {
            return ReadAll(position, Commands.ReadAllBackward);
        }

        private IEnumerable<WyrmItem> ReadAll(Position position, Commands command)
        {
            using (var client = Create())
            using (var stream = client.GetStream())
            using (var reader = new BinaryReader(stream))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write((int) 4 + 4 + 32);
                writer.Write((int) command);
                writer.Write(position.Binary);
                writer.Flush();

                while (true)
                {
                    var length = reader.ReadInt32();
                    var query = (Queries) reader.ReadInt32();
                    var payload = reader.ReadBytes(length - sizeof(Int32) * 2);
                    var tokenizer = new Tokenizer(payload);

                    if (query == Queries.Success)
                    {
                        yield break;
                    }
                    else if (query == Queries.Event)
                    {
                        yield return ParseEvent(tokenizer);
                    }
                    else if (query == Queries.Deleted)
                    {
                        yield return ParseDeletedEvent(tokenizer);
                    }
                    else if (query == Queries.Exception)
                    {
                        ParseException(tokenizer);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
            }
        }

        public IEnumerable<WyrmItem> ReadStreamForward(string streamName)
        {
            return ReadStream(streamName, Commands.ReadStreamForward);
        }

        public IEnumerable<WyrmItem> ReadStreamBackward(string streamName)
        {
            return ReadStream(streamName, Commands.ReadStreamBackward);
        }

        public async Task<Position> CreateStreamAsync(string streamName)
        {
            var bundle = new Bundle
            {
                Items = new List<BundleItem>
                {
                    new CreateBundleItem
                    {
                        StreamName = streamName,
                    }
                }
            };

            return await Append(bundle);
        }

        public async Task<Position> DeleteStreamAsync(string streamName, long version)
        {
            var bundle = new Bundle
            {
                Items = new List<BundleItem>
                {
                    new DeleteBundleItem
                    {
                        StreamName = streamName,
                        StreamVersion = version,
                    }
                }
            };

            return await Append(bundle);
        }

        public Task<Position> Append(Bundle bundle)
        {
            var position = Position.Start;

            using (var client = Create())
            using (var stream = client.GetStream())
            using (var reader = new BinaryReader(stream))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write((int) 4 + 4 + 4 + bundle.Items.Sum(x => x.Count()));
                writer.Write((int) OperationType.PUT);
                writer.Write((int) CommitPolicy.All);

                foreach (var item in bundle.Items)
                {
                    if (item is CreateBundleItem createItem)
                    {
                        writer.Write((int) BundleOp.Create);
                        writer.Write((int) createItem.StreamName.Length);
                        writer.Write(Encoding.UTF8.GetBytes(createItem.StreamName));
                    }
                    else if (item is DeleteBundleItem deleteItem)
                    {
                        writer.Write((int) BundleOp.Delete);
                        writer.Write((long) deleteItem.StreamVersion);
                        writer.Write((int) deleteItem.StreamName.Length);
                        writer.Write(Encoding.UTF8.GetBytes(deleteItem.StreamName));
                    }
                    else if (item is EventsBundleItem eventsItem)
                    {
                        writer.Write((int) BundleOp.Events);
                        writer.Write((long) eventsItem.StreamVersion);
                        writer.Write((int) eventsItem.StreamName.Length);
                        writer.Write(Encoding.UTF8.GetBytes(eventsItem.StreamName));
                        writer.Write((int) eventsItem.Events.Count());

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
                    var payload = reader.ReadBytes(length - sizeof(Int32) * 2);
                    var tokenizer = new Tokenizer(payload);

                    if (query == Queries.Success)
                    {
                        break;
                    }
                    else if (query == Queries.Checkpoint)
                    {
                        position = ParseChecksum(tokenizer);
                    }
                    else if (query == Queries.Exception)
                    {
                        ParseException(tokenizer);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }

                client.Close();
            }

            return Task.FromResult(position);
        }

        public IEnumerable<WyrmItem> SubscribeAll(Position from)
        {
            using (var client = Create())
            using (var stream = client.GetStream())
            using (var reader = new BinaryReader(stream))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write((int) 4 + 4 + 32);
                writer.Write((int) Commands.SubscribeAll);
                writer.Write(from.Binary);
                writer.Flush();

                while (true)
                {
                    var length = reader.ReadInt32();
                    var query = (Queries) reader.ReadInt32();
                    var payload = reader.ReadBytes(length - sizeof(Int32) * 2);
                    var tokenizer = new Tokenizer(payload);

                    if (query == Queries.Success)
                    {
                        yield break;
                    }
                    else if (query == Queries.Event)
                    {
                        yield return ParseEvent(tokenizer);
                    }
                    else if (query == Queries.Deleted)
                    {
                        yield return ParseDeletedEvent(tokenizer);
                    }
                    else if (query == Queries.StreamVersion)
                    {
                        yield return ParseStreamVersion(tokenizer);
                    }
                    else if (query == Queries.Ahead)
                    {
                        yield return ParseAhead(tokenizer);
                        yield break; //oldman
                    }
                    else if (query == Queries.Exception)
                    {
                        ParseException(tokenizer);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
            }
        }

        public IEnumerable<string> EnumerateStreams(params Type[] filters)
        {
            using (var client = Create())
            using (var stream = client.GetStream())
            using (var reader = new BinaryReader(stream))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write((int) 4 + 4);
                writer.Write((int) Commands.ListStreams);
                writer.Flush();

                while (true)
                {
                    var length = reader.ReadInt32();
                    var query = (Queries) reader.ReadInt32();
                    var payload = reader.ReadBytes(length - sizeof(Int32) * 2);
                    var tokenizer = new Tokenizer(payload);

                    if (query == Queries.Success)
                    {
                        yield break;
                    }
                    else if (query == Queries.StreamName)
                    {
                        yield return ParseStreamName(tokenizer);
                    }
                    else if (query == Queries.Exception)
                    {
                        ParseException(tokenizer);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
            }
        }

        public IEnumerable<WyrmItem> EnumerateAllGroupByStream(params Type[] filters)
        {
            using (var client = Create())
            using (var stream = client.GetStream())
            using (var reader = new BinaryReader(stream))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write((int) 4 + 4);
                writer.Write((int) Commands.ReadAllForwardGroupByStream);
                writer.Flush();

                while (true)
                {
                    var length = reader.ReadInt32();
                    var query = (Queries) reader.ReadInt32();
                    var payload = reader.ReadBytes(length - sizeof(Int32) * 2);
                    var tokenizer = new Tokenizer(payload);

                    if (query == Queries.Success)
                    {
                        yield break;
                    }
                    else if (query == Queries.Event)
                    {
                        yield return ParseEvent(tokenizer);
                    }
                    else if (query == Queries.Deleted)
                    {
                        yield return ParseDeletedEvent(tokenizer);
                    }
                    else if (query == Queries.StreamVersion)
                    {
                        yield return ParseStreamVersion(tokenizer);
                    }
                    else if (query == Queries.Exception)
                    {
                        ParseException(tokenizer);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
            }
        }

        public Position Checkpoint()
        {
            var position = Position.Start;

            using (var client = Create())
            using (var stream = client.GetStream())
            using (var reader = new BinaryReader(stream))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write((int) 8);
                writer.Write((int) Commands.Checkpoint);
                writer.Flush();

                while (true)
                {
                    var length = reader.ReadInt32();
                    var query = (Queries) reader.ReadInt32();
                    var payload = reader.ReadBytes(length - sizeof(Int32) * 2);
                    var tokenizer = new Tokenizer(payload);

                    if (query == Queries.Success)
                    {
                        return position;
                    }
                    else if (query == Queries.Checkpoint)
                    {
                        position = ParseChecksum(tokenizer);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
            }
        }
        
        public TimeSpan Ping()
        {
            var watch = Stopwatch.StartNew();
            using (var client = Create())
            using (var stream = client.GetStream())
            using (var reader = new BinaryReader(stream))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write((int) 8);
                writer.Write((int) Commands.Ping);
                writer.Flush();

                while (true)
                {
                    var length = reader.ReadInt32();
                    var query = (Queries) reader.ReadInt32();
                    var payload = reader.ReadBytes(length - sizeof(Int32) * 2);
                    var tokenizer = new Tokenizer(payload);

                    if (query == Queries.Pong)
                    {
                        return TimeSpan.FromMilliseconds(watch.ElapsedMilliseconds);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
            }
        }

        private WyrmItem ParseAhead(Tokenizer tokenizer)
        {
            return new WyrmAheadItem();
        }

        private void ParseException(Tokenizer tokenizer)
        {
            var code = tokenizer.ReadI32();
            var message = tokenizer.ReadString();

            throw new Exception(message);
        }

        private WyrmItem ParseStreamVersion(Tokenizer tokenizer)
        {
            var version = tokenizer.ReadI64();

            return new WyrmVersionItem
            {
                StreamVersion = version,
            };
        }

        private WyrmItem ParseDeletedEvent(Tokenizer tokenizer)
        {
            var time = tokenizer.ReadDateTime();
            var offset = tokenizer.ReadI64();
            var totalOffset = tokenizer.ReadI64();
            var position = tokenizer.ReadBinary(32);
            var streamName = tokenizer.ReadString();
            var eventType = tokenizer.ReadString();

            return new WyrmDeleteItem
            {
                EventType = eventType,
                Offset = offset,
                TotalOffset = totalOffset,
                Position = position,
                StreamName = streamName,
                Serializer = Serializer,
                Timestamp = time,
            };
        }

        private WyrmItem ParseEvent(Tokenizer tokenizer)
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

            return new WyrmEventItem
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

        private string ParseStreamName(Tokenizer tokenizer)
        {
            var streamName = tokenizer.ReadString();

            return streamName;
        }

        private Position ParseChecksum(Tokenizer tokenizer)
        {
            var binary = tokenizer.ReadBinary(32);

            return new Position(binary);
        }
    }
}