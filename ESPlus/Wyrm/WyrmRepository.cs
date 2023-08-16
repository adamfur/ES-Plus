using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ESPlus.Exceptions;
using ESPlus.Extensions;
using ESPlus.Interfaces;

namespace ESPlus.Wyrm
{
    public class WyrmRepository : IRepository
    {
        private readonly IEventSerializer _eventSerializer;
        private readonly IWyrmDriver _wyrmConnection;
        private readonly IWyrmAggregateRenamer _aggregateRenamer;
        private static readonly Dictionary<string, Type> Types = new Dictionary<string, Type>();

        static WyrmRepository()
        {
            var aggregates = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => x.GetTypes())
                .Where(x => x.IsAssignableToGenericType(typeof(IAggregate<>)))
                .ToList();

            foreach (var aggregate in aggregates)
            {
                aggregate.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(x => x.Name == "Apply" && x.ReturnType == typeof(void))
                    // .Where(x => x.GetCustomAttribute(typeof(NoReplayAttribute)) == null)
                    .Select(x => x.GetParameters().First().ParameterType)
                    .ToList()
                    .ForEach(t =>
                    {
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

        public async Task<TAggregate> GetByIdAsync<TAggregate>(string id, CancellationToken cancellationToken = default, long version = Int64.MaxValue)
            where TAggregate : IAggregate<string>
        {
            return await GetByIdAsync<TAggregate, string>(id, cancellationToken, version);
        }

        public Task DeleteAsync(string id, long version = -1, CancellationToken cancellationToken = default)
        {
            return DeleteAsync<string>(id, version, cancellationToken);
        }

        public WyrmTransaction BeginTransaction()
        {
            return new WyrmTransaction(_wyrmConnection, _aggregateRenamer, _eventSerializer);
        }

        public async IAsyncEnumerable<(TAggregate, string)> GetAllByAggregateType<TAggregate>(params Type[] filters)
            where TAggregate : IAggregate
        {
            var aggregate = default(TAggregate);
            var stream = default(string);
            var applyAggregate = default(IAggregate);
            var tenant = default(string);
            var changed = false;

            await foreach (var evnt in _wyrmConnection.EnumerateAllByStreamsAsync(default, filters))
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
                    tenant = metadata?.Tenant;
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
            return (TAggregate) Activator.CreateInstance(typeof(TAggregate), id);
        }
        
        public async Task<WyrmResult> SaveAsync<TAggregate>(TAggregate aggregate, object headers, CancellationToken cancellationToken = default)
            where TAggregate : IAggregate<string>
        {
            return await SaveAsync<string>(aggregate, headers, cancellationToken);
        }

        public async Task<WyrmResult> SaveAsync<T>(IAggregate<T> aggregate, object headers, CancellationToken cancellationToken = default)
        {
            var transaction = BeginTransaction(); 
            
            transaction.Save(aggregate, headers);
            return await transaction.Commit(cancellationToken);
        }
        public async Task<TAggregate> GetByIdAsync<TAggregate, T>(T id, CancellationToken cancellationToken = default, long version = Int64.MaxValue)
            where TAggregate : IAggregate<T>        
        {
            var streamName = _aggregateRenamer.Name(id.ToString());
            var aggregate = ConstructAggregate<TAggregate, T>(id);
            var applyAggregate = (IAggregate<T>) aggregate;
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
                        throw new AggregateInvalidException($"Tried to load {id} which is of another type.");
                    }
                }
                
                var type = Types.Values.FirstOrDefault(x => x.FullName == evnt.EventType);
                
                if (type == null)
                {
                    throw new Exception($"Unknown event type: {evnt.EventType}");
                }

                var @event = _eventSerializer.Deserialize(type, evnt.Data);
                
                any = true;
                applyAggregate.ApplyChange(@event);
                applyAggregate.Version = evnt.Version;
            }

            if (!any)
            {
                throw new AggregateNotFoundException(streamName, null);
            }

            aggregate.TakeUncommittedEvents();

            return aggregate;
        }

        public async Task DeleteAsync<T>(T id, long version = -1, CancellationToken cancellationToken = default)
        {
            var streamName = _aggregateRenamer.Name(id.ToString());

            await _wyrmConnection.DeleteAsync(streamName, version, cancellationToken);
        }

        private static TAggregate ConstructAggregate<TAggregate, T>(T id)
            where TAggregate : IAggregate<T>
        {
            return (TAggregate) Activator.CreateInstance(typeof(TAggregate), id);
        }
    }
}