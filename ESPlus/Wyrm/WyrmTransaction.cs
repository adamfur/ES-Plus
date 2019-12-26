using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ESPlus.Aggregates;
using ESPlus.Interfaces;

namespace ESPlus.Wyrm
{
    public class WyrmTransaction : IRepositoryTransaction
    {
        private List<BundleItem> _bundles = new List<BundleItem>(); 
        private readonly IStore _store;
        private readonly IWyrmDriver _wyrmDriver;

        public WyrmTransaction(IStore store, IWyrmDriver wyrmDriver)
        {
            _store = store;
            _wyrmDriver = wyrmDriver;
        }

        public async Task<WyrmResult> SaveAsync(AggregateBase aggregate, object headers = null,
            long expectedVersion = ExpectedVersion.Specified,
            bool encrypt = true)
        {
            var version = expectedVersion;
            var events = aggregate.TakeUncommittedEvents().ToList();

            if (expectedVersion == ExpectedVersion.Specified)
            {
                version = aggregate.Version;
            }

            var bundleItem = new EventsBundleItem
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
            };
            
            _bundles.Add(bundleItem);
            return WyrmResult.Empty();
        }

        public Task<TAggregate> GetByIdAsync<TAggregate>(string id) where TAggregate : IAggregate
        {
            return _store.GetByIdAsync<TAggregate>(id);
        }

        public async Task<WyrmResult> CreateStreamAsync(string streamName)
        {
            var bundleItem = new CreateBundleItem
            {
                StreamName = streamName,
            };
            
            _bundles.Add(bundleItem);
            return WyrmResult.Empty();
        }

        public async Task<WyrmResult> DeleteStreamAsync(string streamName, long version = -1)
        {
            var bundleItem = new DeleteBundleItem
            {
                StreamName = streamName,
                StreamVersion = version,
            };
            
            _bundles.Add(bundleItem);
            return WyrmResult.Empty();
        }

        public void Dispose()
        {
            _bundles.Clear();
        }

        public Task<WyrmResult> Commit(CommitPolicy policy = CommitPolicy.All)
        {
            return _wyrmDriver.Append(new Bundle
            {
                Encrypt = true, // bad place
                Policy = policy,
                Items = _bundles,
            });
        }
    }
}