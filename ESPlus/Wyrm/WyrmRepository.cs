using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        private object _lock = new object();
        private RepositoryTransaction _transaction = null;

        public WyrmRepository(IWyrmDriver wyrmConnection)
        {
            _wyrmConnection = wyrmConnection;
            _eventSerializer = wyrmConnection.Serializer;
        }

        private void Index<TAggregate>()
        {
            lock (_lock)
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
        }

        private WyrmEvent ToEventData(Guid eventId, object evnt, string streamName, long version, object headers)
        {
            var data = _eventSerializer.Serialize(evnt);
            var metadata = _eventSerializer.Serialize(headers);
            var typeName = evnt.GetType().FullName;

            return new WyrmEvent(eventId, typeName, data, metadata, streamName, (int)version);
        }

        public Task<Position> CreateStreamAsync(string streamName)
        {
            return _wyrmConnection.CreateStreamAsync(streamName);
        }

        public Task<Position> DeleteStreamAsync(string id, long version = -1)
        {
            return _wyrmConnection.DeleteStreamAsync(id, version);
        }

        public Task<Position> SaveAsync(AggregateBase aggregate,
            object headers = null, long savePolicy = ExpectedVersion.Specified)
        {
            var newEvents = ((IAggregate)aggregate).TakeUncommittedEvents().ToList();
            var originalVersion = aggregate.Version - newEvents.Count();
            var expectedVersion = originalVersion == -1 ? ExpectedVersion.NoStream : originalVersion;

            return SaveAggregate(aggregate, newEvents, expectedVersion + 1, headers);
        }

        public Task<Position> AppendAsync(AggregateBase aggregate, object headers)
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

        private async Task<Position> SaveAggregate(IAggregate aggregate, IEnumerable<object> newEvents, long expectedVersion, object headers)
        {
            if (!newEvents.Any())
            {
                return Position.Start;
            }

            var streamName = aggregate.Id;
            var eventsToSave = newEvents.Select((e, ix) => ToEventData(Guid.NewGuid(), e, streamName, Version(expectedVersion, ix), headers)).ToList();

            if (_transaction != null)
            {
                _transaction.Append(eventsToSave);
                return Position.Start;
            }
            else
            {
//                return await _wyrmConnection.Append(eventsToSave);
                throw new NotImplementedException();
            }
        }

        public async Task<TAggregate> GetByIdAsync<TAggregate>(string id, long version = long.MaxValue) where TAggregate : IAggregate
        {
            throw new NotImplementedException();
//            var aggregate = ConstructAggregate<TAggregate>(id);
//            var applyAggregate = (IAggregate)aggregate;
//            bool any = false;
//
//            if (version <= 0)
//            {
//                throw new ArgumentException("Cannot get version < 0");
//            }
//
//            Index<TAggregate>();
//
//            foreach (var evnt in _wyrmConnection.ReadStreamForward(id))
//            {
//                Type type;
//
//                lock (_lock)
//                {
//                    type = _types.Values.FirstOrDefault(x => x.FullName == evnt.EventType);
//                }
//
//                any = true;
//                if (type == null)
//                {
//                    applyAggregate.Version = evnt.Version;
//                    continue;
//                }
//
////                applyAggregate.ApplyChange(_eventSerializer.Deserialize(type, evnt.Data));
////                applyAggregate.Version = evnt.Version;
//            }
//
//            if (!any)
//            {
//                throw new AggregateNotFoundException(id, null);
//            }
//
//            aggregate.TakeUncommittedEvents();
//            await Task.FromResult(0);
//
//            return aggregate;
        }

        public IEnumerable<TAggregate> GetAllByAggregateType<TAggregate>(params Type[] filters) where TAggregate : IAggregate
        {
            throw new NotImplementedException();
//            var aggregate = default(TAggregate);
//            var stream = default(string);
//            var applyAggregate = default(IAggregate);
//
//            Index<TAggregate>();
//            foreach (var evnt in _wyrmConnection.EnumerateAllByStreams(filters))
//            {
//                if (evnt.StreamName != stream)
//                {
//                    if (!ReferenceEquals(aggregate, default(TAggregate)))
//                    {
//                        aggregate.TakeUncommittedEvents();
//                        yield return aggregate;
//                    }
//
//                    stream = evnt.StreamName;
//                    aggregate = ConstructAggregate<TAggregate>(evnt.StreamName);
//                    applyAggregate = aggregate;
//                }
//
//                Type type;
//
//                lock (_lock)
//                {
//                    type = _types.Values.FirstOrDefault(x => x.FullName == evnt.EventType);
//                }
//
//                if (type != null)
//                {
//                    applyAggregate.ApplyChange(_eventSerializer.Deserialize(type, evnt.Data));
//                }
//
//                applyAggregate.Version = evnt.Version;
//
//                if (evnt.IsAhead)
//                {
//                    aggregate.TakeUncommittedEvents();
//                    yield return aggregate;
//                }
//            }
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

//        public async Task<Position> Commit(Interfaces.CommitPolicy policy = CommitPolicy.All)
//        {
//            if (_transaction != null)
//            {
//                var result = await _wyrmConnection.Append(_transaction.Events);
//
//                return result;
//            }
//            else
//            {
//                throw new NotImplementedException();
//            }
//        }
    }
}