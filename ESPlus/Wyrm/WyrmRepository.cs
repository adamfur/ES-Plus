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

        public WyrmRepository(IStore store, IWyrmDriver driver)
        {
            _store = store;
            _driver = driver;
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
            try
            {
                var events = _driver.ReadStreamForward(id);

                if (!events.Any())
                {
                    throw new AggregateNotFoundException(id, typeof(TAggregate));
                }

                var aggregate = ConstructAggregate<TAggregate>(id);

                foreach (var @event in events)
                {
                    @event.Accept(aggregate);
                }
                
                return aggregate;
            }
            catch (WyrmException ex)
            {
                throw new AggregateNotFoundException(id, typeof(TAggregate));
            }
        }
        
        public IEnumerable<TAggregate> GetAllByTypeAsync<TAggregate>()
            where TAggregate : IAggregate
        {
            var aggregate = default(TAggregate);
            var events = _driver.ReadAllGroupByStream();
            
            foreach (var @event in events)
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
            return new WyrmTransaction(_driver);
        }
        
        private static TAggregate ConstructAggregate<TAggregate>(string id)
        {
            return (TAggregate)Activator.CreateInstance(typeof(TAggregate), id);
        }
    }
}
