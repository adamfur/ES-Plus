using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ESPlus.Aggregates;
using ESPlus.Interfaces;

namespace ESPlus.Wyrm
{
    public class WyrmStore : IStore
    {
        private readonly IWyrmDriver _wyrmDriver;

        public WyrmStore(IWyrmDriver wyrmDriver)
        {
            _wyrmDriver = wyrmDriver;
        }

        public async Task<WyrmResult> SaveAsync(AggregateBase aggregate, object headers = null,
            long expectedVersion = ExpectedVersion.Specified,
            bool encrypt = true)
        {
            var version = expectedVersion;
            var events = aggregate.TakeUncommittedEvents().ToList();

            if (!events.Any())
            {
                return WyrmResult.Empty();
            }

            if (expectedVersion == ExpectedVersion.Specified)
            {
                version = aggregate.Version - events.Count + 1;
            }

            return await _wyrmDriver.Append(new Bundle
            {
                Encrypt = encrypt,
                Policy = CommitPolicy.All,
                Items = new List<BundleItem>
                {
                    new EventsBundleItem
                    {
                        StreamName = aggregate.Id,
                        StreamVersion = version,
                        Events = events.Select(x => new BundleEvent
                        {
                            Body = _wyrmDriver.Serializer.Serialize(x),
                            Metadata = _wyrmDriver.Serializer.Serialize(headers),
                            EventId = Guid.NewGuid(),
                            EventType = x.GetType().FullName,
                        }).ToList()
                    }
                }
            });
        }

        public async Task<TAggregate> GetByIdAsync<TAggregate>(string id)
            where TAggregate : IAggregate
        {
            var aggregate = ConstructAggregate<TAggregate>(id);

            foreach (var @event in _wyrmDriver.ReadStreamForward(id))
            {
                @event.Accept(aggregate);
            }
            
            aggregate.TakeUncommittedEvents();
            
            return aggregate;
        }

        public Task<WyrmResult> CreateStreamAsync(string streamName)
        {
            return _wyrmDriver.CreateStreamAsync(streamName);
        }

        public Task<WyrmResult> DeleteStreamAsync(string streamName, long version = -1)
        {
            return _wyrmDriver.DeleteStreamAsync(streamName, version);
        }
        
        private static TAggregate ConstructAggregate<TAggregate>(string id)
        {
            return (TAggregate)Activator.CreateInstance(typeof(TAggregate), id);
        }
    }
}