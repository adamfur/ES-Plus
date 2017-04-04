using System;
using System.Collections.Generic;
using System.Linq;
using ESPlus.Storage;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;

namespace ESPlus.Subscribers
{
    public class EventFetcher : IEventFetcher
    {
        private readonly IEventStoreConnection _eventStoreConnection;
        private readonly UserCredentials _userCredentials;
        private readonly int _blockSize;
        private bool _subscriptionOnline = false;

        public EventFetcher(IEventStoreConnection eventStoreConnection, int blockSize = 512)
        {
            _eventStoreConnection = eventStoreConnection;
            _blockSize = blockSize;
        }

        public IEnumerable<Event> GetFromPosition(long position)
        {
            //Console.WriteLine($"GetFromPosition(long position = {position})");
            var pos = position == -1L ? Position.Start : position.ToPosition();
            var events = _eventStoreConnection.ReadAllEventsForwardAsync(pos, _blockSize, false).Result;

            if (!_subscriptionOnline && events.Events.Count() != _blockSize)
            {
                _subscriptionOnline = true;
                InitializeSubscription(events.NextPosition);
            }

            //var  NextPosition = events.NextPosition.CommitPosition;
            return events.Events.Select(e => new Event() { Position = e.OriginalPosition.Value.CommitPosition });
        }

        private void InitializeSubscription(Position position)
        {
            Console.WriteLine($"SubscribeToAllFrom({position})");
            var settings = new CatchUpSubscriptionSettings(_blockSize, _blockSize, false, false);

            _eventStoreConnection.SubscribeToAllFrom(Position.Start, settings, EventAppeared);
        }

        private void EventAppeared(EventStoreCatchUpSubscription eventStoreSubscription, ResolvedEvent resolvedEvent)
        {
            Console.WriteLine($"PosX: {resolvedEvent.OriginalPosition}, Type: {resolvedEvent.Event.EventType}");
        }        
    }
}