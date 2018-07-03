using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ESPlus.Aggregates;
using ESPlus.Interfaces;
using EventStore.ClientAPI;

namespace ESPlus.Wyrm
{
    public static class OperationType
    {
        public static byte READ_STREAM_FORWARD = 0x01;
        public static byte READ_STREAM_BACKWARD = 0x02;
        public static byte READ_ALL_FORWARD = 0x03;
        public static byte READ_ALL_BACKWARD = 0x04;
        public static byte SUBSCRIBE = 0x05;
        public static byte PUT = 0x06;
        public static byte DELETE = 0x07;
        public static byte FLOOD = (byte)'a';
    }

    public static class ExpectedVersion
    {
        public const long Any = -2;         // This write should not conflict with anything and should always succeed.
        public const long NoStream = -1;    // The stream being written to should not yet exist. If it does exist treat that as a concurrency problem.
        public const long EmptyStream = -1; // The stream should exist and should be empty. If it does not exist or is not empty treat that as a concurrency problem.
        public const long StreamExists = -4;    // The stream should exist. If it or a metadata stream does not exist treat that as a concurrency problem.
    }

    public class WyrmEvent
    {
        public WyrmEvent(Guid eventId, string eventType, byte[] body, byte[] metadata, string streamName, int version)
        {
            EventId = eventId;
            EventType = eventType;
            Body = body;
            Metadata = metadata;
            StreamName = streamName;
            Version = version;
        }

        public Guid EventId { get; }
        public string EventType { get; }
        public byte[] Body { get; }
        public byte[] Metadata { get; }
        public string StreamName { get; }
        public int Version { get; }
    }

    public class WyrmRepository : IRepository
    {
        private readonly IEventSerializer _eventSerializer;
        private readonly WyrmConnection _wyrmConnection;
        private static Dictionary<string, Type> _types = new Dictionary<string, Type>();

        public static void Register<Type>()
        {
            _types[typeof(Type).FullName] = typeof(Type);
        }

        public WyrmRepository(WyrmConnection wyrmConnection, IEventSerializer eventSerializer)
        {
            _wyrmConnection = wyrmConnection;
            _eventSerializer = eventSerializer;
        }

        private void Index<TAggregate>()
        {
            typeof(TAggregate).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(x => x.Name == "Apply" && x.ReturnType == typeof(void))
                .Where(x => x.GetCustomAttribute(typeof(NoReplayAttribute)) == null)
                .Select(x => x.GetParameters().First().ParameterType)
                .ToList()
                .ForEach(t =>
                {
                    //Console.WriteLine($"Register type: {t.FullName}");
                    _types[t.FullName] = t;
                });
        }

        private WyrmEvent ToEventData(Guid eventId, object evnt, string streamName, long version)
        {
            var data = _eventSerializer.Serialize(evnt);
            var metadata = _eventSerializer.Serialize(new object()); //EventMetadata
            var typeName = evnt.GetType().FullName;

            return new WyrmEvent(eventId, typeName, data, metadata, streamName, (int)version);
        }

        public async Task DeleteAsync(string id, long version)
        {
            await _wyrmConnection.DeleteAsync(id);
        }

        public Task SaveAsync(AggregateBase aggregate)
        {
            var newEvents = ((IAggregate)aggregate).TakeUncommittedEvents().ToList();
            var originalVersion = aggregate.Version - newEvents.Count();
            var expectedVersion = originalVersion == -1 ? ExpectedVersion.NoStream : originalVersion;

            return SaveAggregate(aggregate, newEvents, expectedVersion + 1);
        }

        public Task AppendAsync(AggregateBase aggregate)
        {
            var newEvents = ((IAggregate)aggregate).TakeUncommittedEvents();
            var expectedVersion = ExpectedVersion.Any;

            return SaveAggregate(aggregate, newEvents, expectedVersion);
        }

        public int Version(long first, int index)
        {
            if (first == ExpectedVersion.Any)
            {
                return (int)ExpectedVersion.Any;
            }
            else if (first == ExpectedVersion.EmptyStream || first == ExpectedVersion.NoStream)
            {
                if (index == 0)
                {
                    return (int)first;
                }
                else
                {
                    return index;
                }
            }
            else
            {
                return (int)first + index;
            }
        }

        private async Task SaveAggregate(IAggregate aggregate, IEnumerable<object> newEvents, long expectedVersion)
        {
            var streamName = aggregate.Id;
            var eventsToSave = newEvents.Select((e, ix) => ToEventData(Guid.NewGuid(), e, streamName, Version(expectedVersion, ix))).ToList();

            await _wyrmConnection.Append(eventsToSave);
        }

        public async Task<TAggregate> GetByIdAsync<TAggregate>(string id, int version = int.MaxValue) where TAggregate : IAggregate
        {
            var aggregate = ConstructAggregate<TAggregate>(id);
            var applyAggregate = (IAggregate)aggregate;
            bool any = false;

            if (version <= 0)
            {
                throw new ArgumentException("Cannot get version < 0");
            }

            Index<TAggregate>();

            foreach (var evnt in _wyrmConnection.EnumerateStream(id))
            {
                var type = _types.Values.FirstOrDefault(x => x.FullName == evnt.EventType);

                any = true;
                if (type == null)
                {
                    applyAggregate.Version = evnt.Version;
                    continue;
                }


                applyAggregate.ApplyChange((dynamic)_eventSerializer.Deserialize(type, evnt.Data));
                applyAggregate.Version = evnt.Version;
            }

            if (!any)
            {
                throw new AggregateNotFoundException("", null);
            }

            aggregate.TakeUncommittedEvents();
            await Task.FromResult(0);

            return aggregate;
        }

        private static TAggregate ConstructAggregate<TAggregate>(string id)
        {
            return (TAggregate)Activator.CreateInstance(typeof(TAggregate), id);
        }

        public Task SaveNewAsync(IAggregate aggregate)
        {
            throw new NotImplementedException();
        }
    }
}