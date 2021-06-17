using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ESPlus.Aggregates;
using ESPlus.Exceptions;
using ESPlus.Interfaces;

namespace ESPlus.Wyrm
{
    public class WyrmRepository : IRepository
    {
        private readonly IEventSerializer _eventSerializer;
        private readonly IWyrmDriver _wyrmConnection;
        private readonly IWyrmAggregateRenamer _aggregateRenamer;
        private static readonly Dictionary<string, Type> Types = new Dictionary<string, Type>();
        private readonly List<Action<object>> _observers = new List<Action<object>>();

        static WyrmRepository()
        {
            var aggregates = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => x.GetTypes())
                .Where(x => typeof(IAggregate).IsAssignableFrom(x))
                .ToList();

            foreach (var aggregate in aggregates)
            {
                aggregate.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(x => x.Name == "Apply" && x.ReturnType == typeof(void))
                    .Where(x => x.GetCustomAttribute(typeof(NoReplayAttribute)) == null)
                    .Select(x => x.GetParameters().First().ParameterType)
                    .ToList()
                    .ForEach(t =>
                    {
                        //Console.WriteLine($"Register type: {t.FullName}");
                        Types[t.FullName] = t;
                    }); 
            }
        }

        public WyrmRepository(IWyrmDriver wyrmConnection, IWyrmAggregateRenamer aggregateRenamer)
        {
            _wyrmConnection = wyrmConnection;
            _aggregateRenamer = aggregateRenamer;
            _eventSerializer = wyrmConnection.Serializer;
        }

        public void Observe(Action<object> @event)
        {
            _observers.Add(@event);
        }

        private void Notify(object @event)
        {
            foreach (var observer in _observers)
            {
                observer(@event);
            }
        }

        private WyrmAppendEvent ToEventData(Guid eventId, object evnt, string streamName, long version, object headers)
        {
            var data = _eventSerializer.Serialize(evnt);
            var metadata = _eventSerializer.Serialize(headers);
            var typeName = evnt.GetType().FullName;

            return new WyrmAppendEvent(eventId, typeName, data, metadata, streamName, version);
        }

        public async Task DeleteAsync(string id, long version = -1, CancellationToken cancellationToken = default)
        {
            var streamName = _aggregateRenamer.Name(id);
            
            await _wyrmConnection.DeleteAsync(streamName, version, cancellationToken);
        }

        public Task<WyrmResult> SaveAsync(AggregateBase aggregate, object headers = null, CancellationToken cancellationToken = default)
        {
            var newEvents = ((IAggregate)aggregate).TakeUncommittedEvents().ToList();
            var originalVersion = aggregate.Version - newEvents.Count();
            var expectedVersion = originalVersion == -1 ? ExpectedVersion.NoStream : originalVersion;

            return SaveAggregate(aggregate, newEvents, expectedVersion + 1, headers, cancellationToken);
        }

        public Task<WyrmResult> AppendAsync(AggregateBase aggregate, object headers = null, CancellationToken cancellationToken = default)
        {
            var newEvents = ((IAggregate)aggregate).TakeUncommittedEvents();
            var expectedVersion = ExpectedVersion.Any;

            return SaveAggregate(aggregate, newEvents, expectedVersion, headers, cancellationToken);
        }

        private int Version(long first, int index)
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

        private async Task<WyrmResult> SaveAggregate(IAggregate aggregate, IEnumerable<object> newEvents, long expectedVersion, object headers, CancellationToken cancellationToken)
        {
            var copy = newEvents.ToList();
            
            if (!copy.Any())
            {
                return new WyrmResult(Position.Start, 0);
            }

            var streamName = _aggregateRenamer.Name(aggregate.Id);
            var eventsToSave = copy.Select((e, ix) => ToEventData(Guid.NewGuid(), e, streamName, Version(expectedVersion, ix), headers)).ToList();

            foreach (var @event in copy)
            {
                Notify(@event);
            }
                
            return await _wyrmConnection.Append(eventsToSave, cancellationToken);
        }

        public async Task<TAggregate> GetByIdAsync<TAggregate>(string id, CancellationToken cancellationToken = default,
            long version = long.MaxValue) where TAggregate : IAggregate
        {
            var streamName = _aggregateRenamer.Name(id);
            var aggregate = ConstructAggregate<TAggregate>(id);
            var applyAggregate = (IAggregate)aggregate;
            bool any = false;

            if (version < 0)
            {
                throw new ArgumentException("Cannot get version < 0");
            }

            await foreach (var evnt in _wyrmConnection.EnumerateStream(streamName, cancellationToken))
            {
                if (applyAggregate.Version == -1)
                {
                    var initialType = applyAggregate.InitialType();
                    
                    if (initialType != null && evnt.EventType != initialType.FullName)
                    {
                        throw new Exception("Invalid Aggregate");
                    }
                }
                
                var type = Types.Values.FirstOrDefault(x => x.FullName == evnt.EventType);

                any = true;
                if (type == null)
                {
                    throw new Exception($"Unknown event type: {evnt.EventType}");
                }

                var @event = _eventSerializer.Deserialize(type, evnt.Data);
                
                applyAggregate.ApplyChange(@event);
                Notify(@event);
                applyAggregate.Version = evnt.Version;
            }

            if (!any)
            {
                throw new AggregateNotFoundException(streamName, null);
            }

            aggregate.TakeUncommittedEvents();

            return aggregate;
        }

        public async IAsyncEnumerable<(TAggregate, string)> GetAllByAggregateType<TAggregate>(params Type[] filters) where TAggregate : IAggregate
        {
            var aggregate = default(TAggregate);
            var stream = default(string);
            var applyAggregate = default(IAggregate);
            var tenant = default(string);
            var changed = false;

            await foreach (var evnt in _wyrmConnection.EnumerateAllByStreamsAsync(CancellationToken.None, filters))
            {
                if (evnt.StreamName != stream)
                {
                    if (!ReferenceEquals(aggregate, default(TAggregate)))
                    {
                        aggregate.TakeUncommittedEvents();
                        yield return (aggregate, tenant);
                    }

                    var metadata = (MetaObject) evnt.Serializer.Deserialize(typeof(MetaObject), evnt.Metadata);
                    
                    stream = evnt.StreamName;
                    aggregate = ConstructAggregate<TAggregate>(evnt.StreamName);
                    applyAggregate = aggregate;
                    tenant = metadata.Tenant;
                }

                var type = Types.Values.FirstOrDefault(x => x.FullName == evnt.EventType);

                if (type != null)
                {
                    applyAggregate!.ApplyChange(_eventSerializer.Deserialize(type, evnt.Data));
                }

                applyAggregate!.Version = evnt.Version;
                changed = true;

                if (evnt.IsAhead)
                {
                    aggregate!.TakeUncommittedEvents();
                    changed = false;
                    yield return (aggregate, tenant);
                }
            }

            if (changed)
            {
                aggregate!.TakeUncommittedEvents();
                yield return (aggregate, tenant);
            }
        }
        
        private static TAggregate ConstructAggregate<TAggregate>(string id)
        {
            return (TAggregate)Activator.CreateInstance(typeof(TAggregate), id);
        }

        public Task<Position> SaveNewAsync(IAggregate aggregate, object headers, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}