using System;
using System.Collections;
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
        private readonly IStore _store;
        private readonly IWyrmDriver _driver;
        private readonly IEventSerializer _eventSerializer;

        public WyrmRepository(IStore store, IWyrmDriver driver, IEventSerializer eventSerializer)
        {
            _store = store;
            _driver = driver;
            _eventSerializer = eventSerializer;
        }

        public Task<WyrmResult> SaveAsync(AggregateBase aggregate, object headers = null,
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

                await foreach (var @event in _driver.ReadStream(id).EventFilter(aggregate.ApplyTypes()).QueryAsync())
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
        
        public async IAsyncEnumerable<TAggregate> GetAllByTypeAsync<TAggregate>()
            where TAggregate : IAggregate
        {
            var origin = ConstructAggregate<TAggregate>("dummy");
            var aggregate = default(TAggregate);
            
            await foreach (var @event in _driver.GroupByStream()
                .CreateEventFilter(origin.InitialType)
                .EventFilter(origin.ApplyTypes())
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
