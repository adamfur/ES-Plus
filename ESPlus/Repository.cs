using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ESPlus.Aggregates;
using ESPlus.Interfaces;
using EventStore.ClientAPI;

namespace ESPlus
{
    public class GetEventStoreRepository : IRepository
    {
        private readonly IEventStoreConnection _eventStoreConnection;
        private readonly IEventSerializer _eventSerializer;
        private static int ReadPageSize = 512;
        private static int WritePageSize = 512;
        private static Dictionary<string, Type> _types = new Dictionary<string, Type>();

        public static void Register<Type>()
        {
            _types[typeof(Type).FullName] = typeof(Type);
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

        public Task SaveAsync(ReplayableObject aggregate)
        {
            var newEvents = ((IAggregate)aggregate).TakeUncommittedEvents();
            var originalVersion = aggregate.Version - newEvents.Count();
            var expectedVersion = originalVersion == 0 ? ExpectedVersion.NoStream : originalVersion;

            return SaveAggregate(aggregate, newEvents, expectedVersion);
        }

        public Task SaveAsync(AppendableObject aggregate)
        {
            var newEvents = ((IAggregate)aggregate).TakeUncommittedEvents();
            var expectedVersion = ExpectedVersion.Any;

            return SaveAggregate(aggregate, newEvents, expectedVersion);
        }

        private async Task SaveAggregate(IAggregate aggregate, IEnumerable<object> newEvents, long expectedVersion)
        {
            var streamName = StreamName(aggregate.GetType(), aggregate.Id);
            var eventsToSave = newEvents.Select(e => ToEventData(Guid.NewGuid(), e)).ToList();

            if (eventsToSave.Count < WritePageSize)
            {
                await _eventStoreConnection.AppendToStreamAsync(streamName, expectedVersion, eventsToSave);
            }
            else
            {
                var transaction = await _eventStoreConnection.StartTransactionAsync(streamName, expectedVersion);

                var position = 0;

                while (position < eventsToSave.Count)
                {
                    var pageEvents = eventsToSave.Skip(position).Take(WritePageSize);
                    await transaction.WriteAsync(pageEvents);
                    position += WritePageSize;
                }

                await transaction.CommitAsync();
            }
        }

        public async Task<TAggregate> GetByIdAsync<TAggregate>(string id, int version = int.MaxValue) where TAggregate : IAggregate
        {
            if (version <= 0)
            {
                throw new InvalidOperationException("Cannot get version <= 0");
            }

            var streamName = StreamName(typeof (TAggregate), id);
            var aggregate = ConstructAggregate<TAggregate>(streamName);
            var applyAggregate = (IAggregate)aggregate;
            var sliceStart = 0L; //Ignores $StreamCreated--
            StreamEventsSlice currentSlice;
            var currentSliceTask = _eventStoreConnection.ReadStreamEventsForwardAsync(streamName, sliceStart, ReadPageSize, false);

            do
            {
                currentSlice = await currentSliceTask;

                if (currentSlice.Status == SliceReadStatus.StreamNotFound)
                {
                    throw new AggregateNotFoundException(id, typeof(TAggregate));
                }

                if (currentSlice.Status == SliceReadStatus.StreamDeleted)
                {
                    throw new AggregateDeletedException(id, typeof(TAggregate));
                }

                var sliceCount = (int)(sliceStart + ReadPageSize <= version ? ReadPageSize : version - sliceStart + 1);
                sliceStart = currentSlice.NextEventNumber;
                currentSliceTask = _eventStoreConnection.ReadStreamEventsForwardAsync(streamName, sliceStart, sliceCount, false);

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

        private string StreamName(Type type, string id)
        {
            // var result = $"{type.Name}:{id}";

            return id;
        }

        public Task SaveNewAsync(IAggregate aggregate)
        {
            throw new NotImplementedException();
        }
    }
}