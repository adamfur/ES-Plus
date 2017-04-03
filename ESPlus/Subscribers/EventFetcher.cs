using System;
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
            _blockSize = 4;
        }

        public IEnumerable<Event> GetFromPosition(long position)
        {
            Console.WriteLine($"GetFromPosition(long position = {position})");
            var pos = position == -1L ? Position.Start : position.ToPosition();

            var events = _eventStoreConnection.ReadAllEventsForwardAsync(pos, _blockSize, false/*, _userCredentials*/).Result;

            return new EventStream
            {
                NextPosition = events.NextPosition.CommitPosition,
                IsEndOfStream = events.IsEndOfStream,
                Events = events.Events.Select(e => new Event() { Position = e.OriginalPosition.Value.CommitPosition }).ToList(),
            }.Events;
        }
    }
}