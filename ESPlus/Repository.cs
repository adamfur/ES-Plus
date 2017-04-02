using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using ESPlus.Aggregates;
using ESPlus.Interfaces;
using EventStore.ClientAPI;
using Newtonsoft.Json;

namespace ESPlus
{
    public interface ISerializer
    {
        string Serialize<T>(T graph);
        T Deserialize<T>(string buffer);
    }

    public interface IEventSerializer
    {
        byte[] Serialize<T>(T graph);
        object Deserialize(Type type, byte[] buffer);
    }

    public class JsonIndentedSerializer : ISerializer
    {
        public string Serialize<T>(T graph)
        {
            return JsonConvert.SerializeObject(graph, Formatting.Indented);
        }

        public T Deserialize<T>(string buffer)
        {
            return JsonConvert.DeserializeObject<T>(buffer);
        }
    }

    public class EventJsonSerializer : IEventSerializer
    {
        public byte[] Serialize<T>(T graph)
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(graph));
        }

        public object Deserialize(Type type, byte[] buffer)
        {
            return JsonConvert.DeserializeObject(Encoding.UTF8.GetString(buffer), type);
        }
    }

    public class GetEventStoreRepository : IRepository
    {
        private readonly IEventStoreConnection _eventStoreConnection;
        private readonly IEventSerializer _eventSerializer;
        private static int ReadPageSize = 512;
        private static int WritePageSize = 512;
        private static Dictionary<string, Type> _types = new Dictionary<string, Type>();

        public static void Register<Type>()
        {
            _types[typeof (Type).FullName] = typeof (Type);
        }

        public GetEventStoreRepository(IEventStoreConnection eventStoreConnection, IEventSerializer eventSerializer)
        {
            _eventStoreConnection = eventStoreConnection;
            _eventSerializer = eventSerializer;
        }

        private EventData ToEventData(Guid eventId, object evnt)
        {
            var data = _eventSerializer.Serialize(evnt);
            var metadata = _eventSerializer.Serialize(new object()); //EventMetadata

            var typeName = evnt.GetType().FullName;

            return new EventData(eventId, typeName, true, data, metadata);
        }

        public void Save<TAggregate>(TAggregate aggregate) where TAggregate : ReplayableObject
        {
            var newEvents = ((IAggregate)aggregate).TakeUncommittedEvents();
            var originalVersion = aggregate.Version - newEvents.Count();
            var expectedVersion = originalVersion == 0 ? ExpectedVersion.NoStream : originalVersion;

            SaveAggregate<TAggregate>(aggregate, newEvents, expectedVersion);
        }

        public void Append<TAggregate>(TAggregate aggregate) where TAggregate : AppendableObject
        {
            var newEvents = ((IAggregate)aggregate).TakeUncommittedEvents();
            var expectedVersion = ExpectedVersion.Any;

            SaveAggregate<TAggregate>(aggregate, newEvents, expectedVersion);
        }

        private void SaveAggregate<TAggregate>(AggregateBase aggregate, IEnumerable<object> newEvents, long expectedVersion)
        {
            var streamName = StreamName<TAggregate>(aggregate.Id);
            var eventsToSave = newEvents.Select(e => ToEventData(Guid.NewGuid(), e)).ToList();

            if (eventsToSave.Count < WritePageSize)
            {
                _eventStoreConnection.AppendToStreamAsync(streamName, expectedVersion, eventsToSave).Wait();
            }
            else
            {
                var transaction = _eventStoreConnection.StartTransactionAsync(streamName, expectedVersion).Result;

                var position = 0;

                while (position < eventsToSave.Count)
                {
                    var pageEvents = eventsToSave.Skip(position).Take(WritePageSize);
                    transaction.WriteAsync(pageEvents).Wait();
                    position += WritePageSize;
                }

                transaction.CommitAsync().Wait();
            }
        }

        public TAggregate GetById<TAggregate>(string id, int version = int.MaxValue) where TAggregate : ReplayableObject
        {
            if (version <= 0)
            {
                throw new InvalidOperationException("Cannot get version <= 0");
            }

            var streamName = StreamName<TAggregate>(id);
            var aggregate = ConstructAggregate<TAggregate>(streamName);
            var applyAggregate = (IAggregate)aggregate;

            var sliceStart = 0L; //Ignores $StreamCreated--
            StreamEventsSlice currentSlice;

            do
            {
                var sliceCount = (int)(sliceStart + ReadPageSize <= version ? ReadPageSize : version - sliceStart + 1);

                currentSlice = _eventStoreConnection.ReadStreamEventsForwardAsync(streamName, sliceStart, sliceCount, false).Result;

                if (currentSlice.Status == SliceReadStatus.StreamNotFound)
                {
                    throw new AggregateNotFoundException(id, typeof(TAggregate));
                }

                if (currentSlice.Status == SliceReadStatus.StreamDeleted)
                {
                    throw new AggregateDeletedException(id, typeof(TAggregate));
                }

                sliceStart = currentSlice.NextEventNumber;

                foreach (var evnt in currentSlice.Events)
                {
                    var type = _types.Values.First(x => x.FullName == evnt.Event.EventType);

                    applyAggregate.ApplyChange((dynamic)_eventSerializer.Deserialize(type, evnt.OriginalEvent.Data));
                }

            } while (version >= currentSlice.NextEventNumber && !currentSlice.IsEndOfStream);

            if (aggregate.Version != version && version != Int32.MaxValue)
            {
                throw new AggregateVersionException(id, typeof(TAggregate), aggregate.Version, version);
            }

            return aggregate;
        }

        private static TAggregate ConstructAggregate<TAggregate>(string id)
        {
            return (TAggregate)Activator.CreateInstance(typeof(TAggregate), id);
        }

        private string StreamName<TAggregate>(string id)
        {
            var result = $"{typeof(TAggregate).Name}:{id}";

            return result;
        }
    }
}