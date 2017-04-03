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
        private Once _wakeUpSubscription;

        public EventFetcher(IEventStoreConnection eventStoreConnection/*, UserCredentials userCredentials*/, int blockSize = 512)
        {
            _eventStoreConnection = eventStoreConnection;
            //_userCredentials = new UserCredentials("admin", "changeit");
            _blockSize = blockSize;
            _wakeUpSubscription = new Once(() => InitializeSubscription());
        }

        public IEnumerable<Event> GetFromPosition(long position)
        {
            //Console.WriteLine($"GetFromPosition(long position = {position})");
            var pos = position == -1L ? Position.Start : position.ToPosition();

            var events = _eventStoreConnection.ReadAllEventsForwardAsync(pos, _blockSize, false/*, _userCredentials*/).Result;

            //Console.WriteLine($"events.IsEndOfStream: {position} {events.IsEndOfStream}, Next: {events.NextPosition}");
            if (events.Events.Count() != _blockSize)
            {
                _wakeUpSubscription.Execute();
            }

            return new EventStream
            {
                NextPosition = events.NextPosition.CommitPosition,
                IsEndOfStream = events.IsEndOfStream,
                Events = events.Events.Select(e => new Event() { Position = e.OriginalPosition.Value.CommitPosition }).ToList(),
            }.Events;
        }

        private void InitializeSubscription()
        {
            Console.WriteLine("InitializeSubscription()");
            _eventStoreConnection.SubscribeToAllAsync(false, EventAppeared, userCredentials: _userCredentials).Wait();
        }

        private void EventAppeared(EventStoreSubscription eventStoreSubscription, ResolvedEvent resolvedEvent)
        {
            Console.WriteLine($"Pos: {resolvedEvent.OriginalPosition}, Type: {resolvedEvent.Event.EventType}");
        }        
    }
}