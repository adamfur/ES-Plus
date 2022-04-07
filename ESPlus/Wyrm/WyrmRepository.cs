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
    public class WyrmRepository : WyrmGenericRepository, IRepository
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
                    // .Where(x => x.GetCustomAttribute(typeof(NoReplayAttribute)) == null)
                    .Select(x => x.GetParameters().First().ParameterType)
                    .ToList()
                    .ForEach(t =>
                    {
                        //Console.WriteLine($"Register type: {t.FullName}");
                        Types[t.FullName] = t;
                    });
            }
        }

        public WyrmRepository(IWyrmDriver wyrmConnection, IWyrmAggregateRenamer aggregateRenamer) : base(wyrmConnection, aggregateRenamer)
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

        public async Task DeleteAsync(string id, long version = -1, CancellationToken cancellationToken = default)
        {
            await base.DeleteAsync(id, version, cancellationToken);
        }

        public Task<WyrmResult> SaveAsync(AggregateBase aggregate, object headers = null,
            CancellationToken cancellationToken = default)
        {
            return base.SaveAsync<string>(aggregate, headers, cancellationToken);
        }

        public Task<WyrmResult> AppendAsync(AggregateBase aggregate, object headers = null, CancellationToken cancellationToken = default)
        {
            return base.AppendAsync(aggregate, headers, cancellationToken);
        }



        public async Task<TAggregate> GetByIdAsync<TAggregate>(string id, CancellationToken cancellationToken = default,
            long version = long.MaxValue) where TAggregate : IAggregate
        {
            return await base.GetByIdAsync<TAggregate, string>(id, cancellationToken);
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

        public Task<Position> SaveNewAsync(IAggregate aggregate, object headers,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}