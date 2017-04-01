using System.Collections.Generic;
using System.Linq;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;

namespace ESPlus.Subscribers
{
    public class EventStoreFetcher : IEventFetcher
    {
        private readonly IEventStoreConnection _eventStoreConnection;
        public static int BlockSize = 512;
        private readonly UserCredentials _userCredentials;

        public EventStoreFetcher(IEventStoreConnection eventStoreConnection, UserCredentials userCredentials)
        {
            _eventStoreConnection = eventStoreConnection;
            _userCredentials = new UserCredentials("admin", "changeit");//userCredentials;
        }

        public IEnumerable<Event> GetFromPosition(long position)
        {
            return _eventStoreConnection.ReadAllEventsForwardAsync(Position.Start, BlockSize, false, _userCredentials).Result
                .Events
                .Select(e => new Event() { Position = e.OriginalPosition.Value.CommitPosition });
        }
    }
}