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
        private static Dictionary<string, Type> _types = new Dictionary<string, Type>();
        private RepositoryTransaction _transaction = null;
        private List<Action<object>> _observers = new List<Action<object>>();

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
                        _types[t.FullName] = t;
                    }); 
            }
        }

        public WyrmRepository(IWyrmDriver wyrmConnection)
        {
            _wyrmConnection = wyrmConnection;
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

        private WyrmEvent ToEventData(Guid eventId, object evnt, string streamName, long version, object headers)
        {
            var data = _eventSerializer.Serialize(evnt);
            var metadata = _eventSerializer.Serialize(headers);
            var typeName = evnt.GetType().FullName;

            return new WyrmEvent(eventId, typeName, data, metadata, streamName, (int)version);
        }

        public async Task DeleteAsync(string id, long version)
        {
            await _wyrmConnection.DeleteAsync(id, version, CancellationToken.None);
        }

        public Task<WyrmResult> SaveAsync(AggregateBase aggregate, object headers = null)
        {
            var newEvents = ((IAggregate)aggregate).TakeUncommittedEvents().ToList();
            var originalVersion = aggregate.Version - newEvents.Count();
            var expectedVersion = originalVersion == -1 ? ExpectedVersion.NoStream : originalVersion;

            return SaveAggregate(aggregate, newEvents, expectedVersion + 1, headers);
        }

        public Task<WyrmResult> AppendAsync(AggregateBase aggregate, object headers = null)
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

        private async Task<WyrmResult> SaveAggregate(IAggregate aggregate, IEnumerable<object> newEvents, long expectedVersion, object headers)
        {
            var copy = newEvents.ToList();
            
            if (!copy.Any())
            {
                return new WyrmResult(Position.Start, 0);
            }

            var streamName = aggregate.Id;
            var eventsToSave = copy.Select((e, ix) => ToEventData(Guid.NewGuid(), e, streamName, Version(expectedVersion, ix), headers)).ToList();

            if (_transaction != null)
            {
                _transaction.Append(eventsToSave);
                foreach (var @event in copy)
                {
                    Notify(@event);
                }
                
                return new WyrmResult(Position.Start, 0);
            }
            else
            {
                foreach (var @event in copy)
                {
                    Notify(@event);
                }
                
                return await _wyrmConnection.Append(eventsToSave);
            }
        }

        public async Task<TAggregate> GetByIdAsync<TAggregate>(string id, long version = long.MaxValue) where TAggregate : IAggregate
        {
            var aggregate = ConstructAggregate<TAggregate>(id);
            var applyAggregate = (IAggregate)aggregate;
            bool any = false;

            if (version <= 0)
            {
                throw new ArgumentException("Cannot get version < 0");
            }

            await foreach (var evnt in _wyrmConnection.EnumerateStream(id))
            {
                if (applyAggregate.Version == -1)
                {
                    if (evnt.EventType != applyAggregate.InitialType().FullName)
                    {
                        throw new Exception("Invalid Aggregate");
                    }
                }
                
                var type = _types.Values.FirstOrDefault(x => x.FullName == evnt.EventType);

                any = true;
                if (type == null)
                {
                    applyAggregate.Version = evnt.Version;
                    continue;
                }

                var @event = _eventSerializer.Deserialize(type, evnt.Data);
                
                applyAggregate.ApplyChange(@event);
                Notify(@event);
                applyAggregate.Version = evnt.Version;
            }

            if (!any)
            {
                throw new AggregateNotFoundException(id, null);
            }

            aggregate.TakeUncommittedEvents();
            await Task.FromResult(0);

            return aggregate;
        }

        public async IAsyncEnumerable<TAggregate> GetAllByAggregateType<TAggregate>(params Type[] filters) where TAggregate : IAggregate
        {
            var aggregate = default(TAggregate);
            var stream = default(string);
            var applyAggregate = default(IAggregate);

            await foreach (var evnt in _wyrmConnection.EnumerateAllByStreamsAsync(CancellationToken.None, filters))
            {
                if (evnt.StreamName != stream)
                {
                    if (!ReferenceEquals(aggregate, default(TAggregate)))
                    {
                        aggregate.TakeUncommittedEvents();
                        yield return aggregate;
                    }

                    stream = evnt.StreamName;
                    aggregate = ConstructAggregate<TAggregate>(evnt.StreamName);
                    applyAggregate = aggregate;
                }

                var type = _types.Values.FirstOrDefault(x => x.FullName == evnt.EventType);

                if (type != null)
                {
                    applyAggregate.ApplyChange(_eventSerializer.Deserialize(type, evnt.Data));
                }

                applyAggregate.Version = evnt.Version;

                if (evnt.IsAhead)
                {
                    aggregate.TakeUncommittedEvents();
                    yield return aggregate;
                }
            }
        }
        
        public async IAsyncEnumerable<TAggregate> GetAllByAggregateType2<TAggregate>(params Type[] filters) where TAggregate : IAggregate
        {
            var shard = ulong.Parse(Environment.GetEnvironmentVariable("SHARD") ?? "0");
            var shards = ulong.Parse(Environment.GetEnvironmentVariable("SHARDS") ?? "6");
            var aggregate = default(TAggregate);
            var stream = default(string);
            var applyAggregate = default(IAggregate);

            await foreach (var evnt in _wyrmConnection.EnumerateAllByStreamsAsync(CancellationToken.None, filters))
            {
                var hash = (ulong) evnt.StreamName.XXH64();

                if ((hash % shards) != shard)
                {
                    continue;
                }
                
                if (evnt.StreamName != stream)
                {
                    if (!ReferenceEquals(aggregate, default(TAggregate)))
                    {
                        aggregate.TakeUncommittedEvents();
                        yield return aggregate;
                    }

                    stream = evnt.StreamName;
                    aggregate = ConstructAggregate<TAggregate>(evnt.StreamName);
                    applyAggregate = aggregate;
                }

                var type = _types.Values.FirstOrDefault(x => x.FullName == evnt.EventType);

                if (type != null)
                {
                    applyAggregate.ApplyChange(_eventSerializer.Deserialize(type, evnt.Data));
                }

                applyAggregate.Version = evnt.Version;

                if (evnt.IsAhead)
                {
                    aggregate.TakeUncommittedEvents();
                    yield return aggregate;
                }
            }
        }

        private static TAggregate ConstructAggregate<TAggregate>(string id)
        {
            return (TAggregate)Activator.CreateInstance(typeof(TAggregate), id);
        }

        public Task<Position> SaveNewAsync(IAggregate aggregate, object headers)
        {
            throw new NotImplementedException();
        }

        public IRepositoryTransaction BeginTransaction()
        {
            var currentTransaction = _transaction;
            var transaction = new RepositoryTransaction(this, () => _transaction = currentTransaction);

            _transaction = transaction;
            return transaction;
        }

        public async Task<WyrmResult> Commit()
        {
            if (_transaction != null)
            {
                var result = await _wyrmConnection.Append(_transaction.Events);

                return result;
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}