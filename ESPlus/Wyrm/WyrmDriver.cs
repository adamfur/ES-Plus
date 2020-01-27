using System;
using System.Collections.Generic;
using System.Data.HashFunction.xxHash;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ESPlus.Wyrm
{
    public class WyrmDriver : IWyrmDriver
    {
        private readonly string _apiKey;
        private readonly string _host;
        private readonly int _port;
        private readonly IxxHash _algorithm;
        
        public IEventSerializer Serializer { get; }

        public WyrmDriver(string connectionString, IEventSerializer eventSerializer, string apiKey = null)
        {
            _apiKey = apiKey;
            var parts = connectionString.Split(":");

            _host = parts[0];
            _port = int.Parse(parts[1]);
            Serializer = eventSerializer;
            _algorithm = xxHashFactory.Instance.Create(new xxHashConfig { HashSizeInBits = 64 });
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
                Authenticate(writer);
                writer.Write((int) 12 + streamName.Length);
                writer.Write((int) command);
                writer.Write((int) streamName.Length);
                writer.Write(Encoding.UTF8.GetBytes(streamName));
                writer.Flush();

                while (true)
                {
                    var (query, tokenizer) = reader.Query();

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
                Authenticate(writer);
                writer.Write((int) 40);
                writer.Write((int) command);
                writer.Write(position.Binary);
                writer.Flush();

                while (true)
                {
                    var (query, tokenizer) = reader.Query();

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

        public async Task<WyrmResult> CreateStreamAsync(string streamName)
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

        public async Task<WyrmResult> DeleteStreamAsync(string streamName, long version)
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

        public Task<WyrmResult> Append(Bundle bundle)
        {
            var position = Position.Start;
            long offset = 0;
            
            using (var client = Create())
            using (var stream = client.GetStream())
            using (var reader = new BinaryReader(stream))
            using (var writer = new BinaryWriter(stream))
            {
                Authenticate(writer);
                writer.Write((int) 12 + bundle.Items.Sum(x => x.Count()));
                writer.Write((int) Commands.Put);
                writer.Write((int) bundle.Policy);

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
                        var eventsCount = eventsItem.Events.Count;

                        if (eventsCount == 0)
                        {
                            continue;
                        }
                        
                        writer.Write((int) BundleOp.Events);
                        writer.Write((long) eventsItem.StreamVersion);
                        writer.Write((int) eventsItem.StreamName.Length);
                        writer.Write(Encoding.UTF8.GetBytes(eventsItem.StreamName));
                        writer.Write((bool) eventsItem.Encrypt);
                        writer.Write((int) eventsCount);

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
                    var (query, tokenizer) = reader.Query();

                    if (query == Queries.Success)
                    {
                        break;
                    }
                    else if (query == Queries.Checkpoint)
                    {
                        position = ParseCheckpoint(tokenizer);
                    }
                    else if (query == Queries.Exception)
                    {
                        ParseException(tokenizer);
                    }
                    else if (query == Queries.TotalOffset)
                    {
                        offset = ParseTotalOffset(tokenizer);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
            }

            return Task.FromResult(new WyrmResult(position, offset));
        }

        public IEnumerable<WyrmItem> SubscribeAll(Position from)
        {
            using (var client = Create())
            using (var stream = client.GetStream())
            using (var reader = new BinaryReader(stream))
            using (var writer = new BinaryWriter(stream))
            {
                Authenticate(writer);
                writer.Write((int) 40);
                writer.Write((int) Commands.ReadAllForwardFollow);
                writer.Write(from.Binary);
                writer.Flush();

                while (true)
                {
                    var (query, tokenizer) = reader.Query();

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

        public IEnumerable<WyrmItem> SubscribeStream(string streamName)
        {
            using (var client = Create())
            using (var stream = client.GetStream())
            using (var reader = new BinaryReader(stream))
            using (var writer = new BinaryWriter(stream))
            {
                Authenticate(writer);
                writer.Write((int) 12 + streamName.Length);
                writer.Write((int) Commands.ReadStreamForwardFollow);
                writer.Write((int) streamName.Length);
                writer.Write(Encoding.UTF8.GetBytes(streamName));
                writer.Flush();

                while (true)
                {
                    var (query, tokenizer) = reader.Query();

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
                Authenticate(writer);
                CreateFilter(writer, filters);
                writer.Write((int) 8);
                writer.Write((int) Commands.ListStreams);
                writer.Flush();

                while (true)
                {
                    var (query, tokenizer) = reader.Query();

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

        private void CreateFilter(BinaryWriter writer, Type[] filters)
        {
            Filters(writer, filters, Commands.CreateFilter);
        }

        private void EventFilter(BinaryWriter writer, Type[] filters)
        {
            Filters(writer, filters, Commands.EventFilter);
        }

        private void Filters(BinaryWriter writer, Type[] filters, Commands command)
        {
            if (!filters.Any())
            {
                return;
            }
            
            writer.Write((int) 12 + filters.Length * 8);
            writer.Write((int) command);
            writer.Write((int) filters.Length);
            
            foreach (var filter in filters)
            {
                var hash = _algorithm.ComputeHash(Encoding.UTF8.GetBytes(filter.FullName)).Hash;
                var i64 = BitConverter.ToInt64(hash);
                
                writer.Write(i64);
            }
        }

        public IEnumerable<WyrmItem> ReadAllGroupByStream(params Type[] filters)
        {
            using (var client = Create())
            using (var stream = client.GetStream())
            using (var reader = new BinaryReader(stream))
            using (var writer = new BinaryWriter(stream))
            {
                Authenticate(writer);
                writer.Write((int) 8);
                writer.Write((int) Commands.ReadAllForwardGroupByStream);
                writer.Flush();

                while (true)
                {
                    var (query, tokenizer) = reader.Query();

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
                    else if (query == Queries.Ahead)
                    {
                        yield return ParseStreamAhead(tokenizer);
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
                Authenticate(writer);
                writer.Write((int) 8);
                writer.Write((int) Commands.Checkpoint);
                writer.Flush();

                while (true)
                {
                    var (query, tokenizer) = reader.Query();

                    if (query == Queries.Success)
                    {
                        return position;
                    }
                    else if (query == Queries.Checkpoint)
                    {
                        position = ParseCheckpoint(tokenizer);
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
                    var (query, tokenizer) = reader.Query();

                    if (query == Queries.Pong)
                    {
                        return TimeSpan.FromMilliseconds(watch.ElapsedMilliseconds);
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

        public void Reset()
        {
            using (var client = Create())
            using (var stream = client.GetStream())
            using (var reader = new BinaryReader(stream))
            using (var writer = new BinaryWriter(stream))
            {
                Authenticate(writer);
                writer.Write((int) 8);
                writer.Write((int) Commands.Reset);
                writer.Flush();

                while (true)
                {
                    var (query, tokenizer) = reader.Query();

                    if (query == Queries.Success)
                    {
                        break;
                    }
                    else if (query == Queries.Exception)
                    {
                        ParseException(tokenizer);
                    }
                    else if (query == Queries.Checkpoint)
                    {
                        ParseCheckpoint(tokenizer);
                    }
                    else if (query == Queries.TotalOffset)
                    {
                        ParseTotalOffset(tokenizer);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
            }
        }

        private void Authenticate(BinaryWriter writer)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                return;
            }

            writer.Write((int) 12 + _apiKey.Length);
            writer.Write((int) Commands.AuthenticateApiKey);
            writer.Write((int) _apiKey.Length);
            writer.Write(Encoding.UTF8.GetBytes(_apiKey));
        }

        private WyrmItem ParseAhead(Tokenizer tokenizer)
        {
            return new WyrmAheadItem();
        }

        private WyrmItem ParseStreamAhead(Tokenizer tokenizer)
        {
            return new WyrmAheadItem();
        }

        private void ParseException(Tokenizer tokenizer)
        {
            var code = tokenizer.ReadI32();
            var message = tokenizer.ReadString();
            var info = tokenizer.ReadString();

            throw new WyrmException(code, message, info);
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
            var position = tokenizer.ReadBinary(32).ToArray();
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
            var position = tokenizer.ReadBinary(32).ToArray();
            var eventId = tokenizer.ReadGuid();
            var eventType = tokenizer.ReadString();
            var streamName = tokenizer.ReadString();
            var metadataLength = tokenizer.ReadI32();
            var dataLength = tokenizer.ReadI32();
            var metadata = tokenizer.ReadBinary(metadataLength).ToArray();
            var data = tokenizer.ReadBinary(dataLength).ToArray();

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

        private Position ParseCheckpoint(Tokenizer tokenizer)
        {
            var binary = tokenizer.ReadBinary(32);

            return new Position(binary.ToArray());
        }

        private long ParseTotalOffset(Tokenizer tokenizer)
        {
            var totalOffset = tokenizer.ReadI64();

            return totalOffset;
        }
    }
}