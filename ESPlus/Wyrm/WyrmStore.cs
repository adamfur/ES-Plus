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
        private readonly IEventSerializer _eventSerializer;

        public WyrmStore(IWyrmDriver wyrmDriver, IEventSerializer eventSerializer)
        {
            _wyrmDriver = wyrmDriver;
            _eventSerializer = eventSerializer;
        }

        public async Task<WyrmResult> SaveAsync(IAggregate aggregate, object headers = null,
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

            return await Apply(
                new EventsBundleItem
                {
                    StreamName = aggregate.Id,
                    StreamVersion = version,
                    Encrypt = encrypt,
                    Events = events.Select(x => new BundleEvent
                    {
                        Body = _eventSerializer.Serialize(x),
                        Metadata = _eventSerializer.Serialize(headers),
                        EventId = Guid.NewGuid(),
                        EventType = x.GetType().FullName,
                    }).ToList()
                });
        }

        public Task<WyrmResult> CreateStreamAsync(string streamName)
        {
            return Apply(new CreateBundleItem
            {
                StreamName = streamName,
            });
        }

        public Task<WyrmResult> DeleteStreamAsync(string streamName, long version = -1)
        {
            return Apply(new DeleteBundleItem
            {
                StreamName = streamName,
                StreamVersion = version,
            });
        }

        protected virtual async Task<WyrmResult> Apply(BundleItem item)
        {
            return await _wyrmDriver.AppendAsync(new Bundle
            {
                Items = new List<BundleItem> {item},
                Policy = CommitPolicy.All,
            });
        }
    }
}