using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ESPlus.Interfaces;

namespace ESPlus.Wyrm
{
    public class WyrmTransaction
    {
        private readonly IWyrmDriver _driver;
        private readonly IWyrmAggregateRenamer _renamer;
        private readonly IEventSerializer _eventSerializer;
        private readonly List<WyrmAppendEvent> _eventsToSave = new List<WyrmAppendEvent>();

        public WyrmTransaction(IWyrmDriver driver, IWyrmAggregateRenamer renamer, IEventSerializer eventSerializer)
        {
            _driver = driver;
            _renamer = renamer;
            _eventSerializer = eventSerializer;
        }
        
        public void Save<T>(IAggregate<T> aggregate, object headers)
        {
            var newEvents = aggregate.TakeUncommittedEvents().ToList();
            var originalVersion = aggregate.Version - newEvents.Count;
            var expectedVersion = originalVersion == -1 ? ExpectedVersion.NoStream : originalVersion;
            
            SaveAggregate(aggregate, newEvents, expectedVersion + 1, headers);
        }

        private void SaveAggregate<T>(IAggregate<T> aggregate, List<object> newEvents, long expectedVersion, object headers)
        {
            var streamName = _renamer.Name(aggregate.Id.ToString());
            var eventsToSave = newEvents.Select((e, ix) => ToEventData(Guid.NewGuid(), e, streamName, Version(expectedVersion, ix), headers)).ToList();

            _eventsToSave.AddRange(eventsToSave);
        }
        
        private WyrmAppendEvent ToEventData(Guid eventId, object evnt, string streamName, long version, object headers)
        {
            var data = _eventSerializer.Serialize(evnt);
            var metadata = _eventSerializer.Serialize(headers);
            var typeName = evnt.GetType().FullName;

            return new WyrmAppendEvent(eventId, typeName, data, metadata, streamName, version);
        }

        public async Task<WyrmResult> Commit(CancellationToken cancellationToken = default)
        {
            if (!_eventsToSave.Any())
            {
                return new WyrmResult(Position.Start, 0);
            }

            var result = await _driver.Append(_eventsToSave, cancellationToken);
            
            _eventsToSave.Clear();
            return result;
        }

        private int Version(long first, int index)
        {
            if (first == ExpectedVersion.Any)
            {
                return (int) ExpectedVersion.Any;
            }
            else if (first == ExpectedVersion.EmptyStream || first == ExpectedVersion.NoStream)
            {
                if (index == 0)
                {
                    return (int) first;
                }
                else
                {
                    return index;
                }
            }
            
            return (int) first + index;
        }
    }
}