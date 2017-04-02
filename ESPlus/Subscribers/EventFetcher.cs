using System.Collections.Generic;
using System.Linq;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;

namespace ESPlus.Subscribers
{
    public class EventFetcher : IEventFetcher
    {
        private readonly IEventStoreConnection _eventStoreConnection;
        private readonly UserCredentials _userCredentials;
        private readonly int _blockSize;

        public EventFetcher(IEventStoreConnection eventStoreConnection/*, UserCredentials userCredentials*/, int blockSize = 512)
        {
            _eventStoreConnection = eventStoreConnection;
            //_userCredentials = new UserCredentials("admin", "changeit");
            _blockSize = blockSize;
        }

        public IEnumerable<Event> GetFromPosition(long position)
        {
            //position.ToPosition()
            return _eventStoreConnection.ReadAllEventsForwardAsync(Position.Start, _blockSize, false/*, _userCredentials*/).Result
                .Events
                .Select(e => new Event() { Position = e.OriginalPosition.Value.CommitPosition });
        }
    }
}