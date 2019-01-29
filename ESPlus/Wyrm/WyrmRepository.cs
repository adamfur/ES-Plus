using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ESPlus.Aggregates;
using ESPlus.Interfaces;
using Newtonsoft.Json;

namespace ESPlus.Wyrm
{
    public class WyrmRepository : IRepository
    {
        private readonly IEventSerializer _eventSerializer;
        private readonly WyrmDriver _wyrmConnection;
        private static Dictionary<string, Type> _types = new Dictionary<string, Type>();

        public static void Register<Type>()
        {
            _types[typeof(Type).FullName] = typeof(Type);
        }

        public WyrmRepository(WyrmDriver wyrmConnection)
        {
            _wyrmConnection = wyrmConnection;
            _eventSerializer = wyrmConnection.Serializer;
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

        private WyrmEvent ToEventData(Guid eventId, object evnt, string streamName, long version, object headers)
        {
            var data = _eventSerializer.Serialize(evnt);
            var metadata = _eventSerializer.Serialize(headers);
            var typeName = evnt.GetType().FullName;

            return new WyrmEvent(eventId, typeName, data, metadata, streamName, (int)version);
        }

        public async Task DeleteAsync(string id, long version)
        {
            await _wyrmConnection.DeleteAsync(id);
        }

        public Task SaveAsync(AggregateBase aggregate, object headers)
        {
            var newEvents = ((IAggregate)aggregate).TakeUncommittedEvents().ToList();
            var originalVersion = aggregate.Version - newEvents.Count();
            var expectedVersion = originalVersion == -1 ? ExpectedVersion.NoStream : originalVersion;

            return SaveAggregate(aggregate, newEvents, expectedVersion + 1, headers);
        }

        public Task AppendAsync(AggregateBase aggregate, object headers)
        {
            var newEvents = ((IAggregate)aggregate).TakeUncommittedEvents();
            var expectedVersion = ExpectedVersion.Any;

            return SaveAggregate(aggregate, newEvents, expectedVersion, headers);
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

        private async Task SaveAggregate(IAggregate aggregate, IEnumerable<object> newEvents, long expectedVersion, object headers)
        {
            if (!newEvents.Any())
            {
                return;
            }

            var streamName = aggregate.Id;
            var eventsToSave = newEvents.Select((e, ix) => ToEventData(Guid.NewGuid(), e, streamName, Version(expectedVersion, ix), headers)).ToList();

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

                applyAggregate.ApplyChange(_eventSerializer.Deserialize(type, evnt.Data));
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

        public Task SaveNewAsync(IAggregate aggregate, object headers)
        {
            throw new NotImplementedException();
        }
    }
}