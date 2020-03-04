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
    public class WyrmDriverExp : IWyrmDriverExp
    {
        private readonly string _apiKey;
        private readonly string _host;
        private readonly int _port;
        private readonly IxxHash _algorithm;
        
        public IEventSerializer Serializer { get; }

        public WyrmDriverExp(string connectionString, IEventSerializer eventSerializer, string apiKey = null)
        {
            _apiKey = apiKey;
            var parts = connectionString.Split(":");

            _host = parts[0];
            _port = int.Parse(parts[1]);
            Serializer = eventSerializer;
            _algorithm = xxHashFactory.Instance.Create(new xxHashConfig { HashSizeInBits = 64 });
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

            return await AppendAsync(bundle);
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

            return await AppendAsync(bundle);
        }

        private async Task<TcpClient> Create()
        {
            var client = new TcpClient();
            client.NoDelay = false;

            await Retry(() => client.Connect(_host, _port));

            return client;
        }

        private async Task Retry(Action action)
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
                    await Task.Delay(TimeSpan.FromSeconds(1 << tries));
                }
            }

            throw exception;
        }
        
        public async Task<WyrmResult> AppendAsync(Bundle bundle)
        {
            var position = Position.Begin;
            long offset = 0;
            
            using (var client = await Create())
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

                await stream.FlushAsync();

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

            return new WyrmResult(position, offset);
        }

        public IWyrmReadPipeline ReadFrom(Position position)
        {
            return new WyrmReadPipeline(this, position);
        }

        public IWyrmReadPipeline ReadStream(string streamName)
        {
            return new WyrmReadPipeline(this, streamName);
        }

        public async IAsyncEnumerable<string> EnumerateStreams()
        {
            using (var client = await Create())
            using (var stream = client.GetStream())
            using (var reader = new BinaryReader(stream))
            using (var writer = new BinaryWriter(stream))
            {
                Authenticate(writer);
                writer.Write((int) 8);
                writer.Write((int) Commands.ListStreams);
                writer.Flush();

                await stream.FlushAsync();

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

        public async Task<Position> CheckpointAsync()
        {
            var position = Position.Begin;

            using (var client = await Create())
            using (var stream = client.GetStream())
            using (var reader = new BinaryReader(stream))
            using (var writer = new BinaryWriter(stream))
            {
                Authenticate(writer);
                writer.Write((int) 8);
                writer.Write((int) Commands.Checkpoint);

                await stream.FlushAsync();

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

        public async Task<TimeSpan> PingAsync()
        {
            var watch = Stopwatch.StartNew();
            using (var client = await Create())
            using (var stream = client.GetStream())
            using (var reader = new BinaryReader(stream))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write((int) 8);
                writer.Write((int) Commands.Ping);

                await stream.FlushAsync();

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

        protected internal IAsyncEnumerable<WyrmItem> ReadQueryAsync(Position position, bool subscribe, string regex,
            List<Type> createEventFilter, List<Type> eventFilter, int take, bool groupByStream, Direction direction,
            int skip)
        {
            return ReadQueryAsync(writer =>
            {
                Authenticate(writer);
                writer.Write((int) 40);
                writer.Write((int) Commands.ReadFrom);
                writer.Write(position.Binary);
            }, subscribe, regex, createEventFilter, eventFilter, take, groupByStream, direction, skip);            
        }

        protected internal IAsyncEnumerable<WyrmItem> ReadQueryAsync(string streamName, bool subscribe, string regex,
            List<Type> createEventFilter, List<Type> eventFilter, int take, bool groupByStream, Direction direction,
            int skip)
        {
            return ReadQueryAsync(writer =>
            {
                writer.Write((int) 12 + streamName.Length);
                writer.Write((int) Commands.ReadStream);
                writer.Write((int) streamName.Length);
                writer.Write(Encoding.UTF8.GetBytes(streamName));
            }, subscribe, regex, createEventFilter, eventFilter, take, groupByStream, direction, skip);
        }
        
        private async IAsyncEnumerable<WyrmItem> ReadQueryAsync(Action<BinaryWriter> action, bool subscribe, string regex, List<Type> createEventFilter, List<Type> eventFilter, int take, bool groupByStream, Direction direction, int skip)
        {
            using (var client = await Create())
            using (var stream = client.GetStream())
            using (var reader = new BinaryReader(stream))
            using (var writer = new BinaryWriter(stream))
            {
                Authenticate(writer);
                action(writer);

                if (subscribe)
                {
                    writer.Write((int) 8);
                    writer.Write((int) Commands.Subscribe);
                }

                if (regex != null)
                {
                    writer.Write((int) 12 + regex.Length);
                    writer.Write((int) Commands.RegexFilter);
                    writer.Write((int) regex.Length);
                    writer.Write(Encoding.UTF8.GetBytes(regex));
                }

                AddFilter(createEventFilter, writer, Commands.CreateEventFilter);
                AddFilter(eventFilter, writer, Commands.EventFilter);
                
                if (take != -1)
                {
                    writer.Write((int) 12);
                    writer.Write((int) Commands.Take);
                    writer.Write((int) take);
                }         
                
                if (skip != -1)
                {
                    writer.Write((int) 12);
                    writer.Write((int) Commands.Skip);
                    writer.Write((int) take);
                }     
                
                if (direction != Direction.Forward)
                {
                    writer.Write((int) 12);
                    writer.Write((int) Commands.Direction);
                    writer.Write((int) direction);
                }

                if (groupByStream)
                {
                    writer.Write((int) 8);
                    writer.Write((int) Commands.GroupByStream);
                }
                
                writer.Write((int) 8);
                writer.Write((int) Commands.ExecuteQuery);

                await stream.FlushAsync();

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

        private void AddFilter(List<Type> filters, BinaryWriter writer, Commands command)
        {
            if (filters == null)
            {
                return;
            }
            
            writer.Write((int) 12 + filters.Count * 8);
            writer.Write((int) command);
            writer.Write((int) filters.Count);

            foreach (var filter in filters)
            {
                var hash = _algorithm.ComputeHash(Encoding.UTF8.GetBytes(filter.FullName)).Hash;
                var i64 = BitConverter.ToInt64(hash);

                writer.Write(i64);
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
        
        public async Task Reset()
        {
            using (var client = await Create())
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
    }
}