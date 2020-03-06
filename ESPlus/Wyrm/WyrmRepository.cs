using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ESPlus.Aggregates;
using ESPlus.Exceptions;
using ESPlus.Interfaces;
using MongoDB.Driver;

namespace ESPlus.Wyrm
{
    public class WyrmRepository : IRepository
    {
        private readonly IStore _store;
        private readonly IWyrmDriver _driver;
        private readonly IEventSerializer _eventSerializer;

        public WyrmRepository(IStore store, IWyrmDriver driver, IEventSerializer eventSerializer)
        {
            _store = store;
            _driver = driver;
            _eventSerializer = eventSerializer;
        }

        public Task<WyrmResult> SaveAsync(IAggregate aggregate, object headers = null,
            long expectedVersion = ExpectedVersion.Specified,
            bool encrypt = true)
        {
            return _store.SaveAsync(aggregate, headers, expectedVersion, encrypt);
        }

        public async Task<TAggregate> GetByIdAsync<TAggregate>(string id)
            where TAggregate : IAggregate
        {
            var any = false;

            try
            {
                var aggregate = ConstructAggregate<TAggregate>(id);

                await foreach (var @event in _driver.ReadStream(id).EventFilter(TypeResolver.Resolve(typeof(TAggregate))).QueryAsync())
                {
                    any = true;
                    @event.Accept(aggregate);
                }

                if (!any)
                {
                    throw new AggregateNotFoundException(id, typeof(TAggregate));
                }

                return aggregate;
            }
            catch (AggregateNotFoundException)
            {
                throw;
            }
            catch (WyrmException ex)
            {
                throw new AggregateNotFoundException(id, typeof(TAggregate));
            }
        }

        private Type InheritedType<TAggregate>()
            where TAggregate : IAggregate
        {
            var type = typeof(TAggregate);
            var interfaceType = type
                .GetInterfaces()
                .First(x => x.GetGenericTypeDefinition() == typeof(AggregateBase<>));
            var genericArguments = interfaceType.GetGenericArguments();
            var firstGenericArgument = genericArguments.First();

            return firstGenericArgument;
        }
        
        public async IAsyncEnumerable<TAggregate> GetAllByTypeAsync<TAggregate>()
            where TAggregate : IAggregate
        {
            var origin = ConstructAggregate<TAggregate>("dummy");
            var aggregate = default(TAggregate);
            
            await foreach (var @event in _driver.ReadGroupByStream()
                .CreateEventFilter(InheritedType<TAggregate>())
                .EventFilter(TypeResolver.Resolve(typeof(TAggregate)))
                .QueryAsync())
            {
                if (@event is WyrmAheadItem)
                {
                    if (aggregate != null)
                    {
                        aggregate.TakeUncommittedEvents();

                        yield return aggregate;

                        aggregate = default;
                    }
                }
                else if (@event is WyrmEventItem eventItem)
                {
                    if (aggregate == null)
                    {
                        aggregate = ConstructAggregate<TAggregate>(eventItem.StreamName);
                    }
                    
                    @event.Accept(aggregate);
                }
            }
        }

        public Task<WyrmResult> CreateStreamAsync(string streamName)
        {
            return _store.CreateStreamAsync(streamName);
        }

        public Task<WyrmResult> DeleteStreamAsync(string streamName, long version = -1)
        {
            return _store.DeleteStreamAsync(streamName, version);
        }

        public IRepositoryTransaction BeginTransaction()
        {
            return new WyrmTransaction(_driver, _eventSerializer);
        }
        
        private static TAggregate ConstructAggregate<TAggregate>(string id)
        {
            return (TAggregate)Activator.CreateInstance(typeof(TAggregate), id);
        }
    }
}
