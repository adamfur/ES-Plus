using System;
using System.Linq;
using System.Threading;
using EventStore.ClientAPI;

namespace ESPlus.Subscribers
{
    public class EventFetcher : IEventFetcher
    {
        private readonly IEventStoreConnection _eventStoreConnection;
        private readonly int _blockSize;
        private bool _subscriptionOnline = false;
        private Action _eventReceivedTrigger = () => {};

        public EventFetcher(IEventStoreConnection eventStoreConnection, int blockSize = 512)
        {
            _eventStoreConnection = eventStoreConnection;
            _blockSize = blockSize;
        }

        public void OnEventReceived(Action action)
        {
            _eventReceivedTrigger = action;
        }

        public EventStream GetFromPosition(Position position)
        {
            //if (position.CommitPosition == 183654176L)
               // Console.WriteLine($"{Thread.CurrentThread.ManagedThreadId} {DateTime.Now:yyyy-MM-dd hh:mm:ss}: EventFetcher(Position position = {position})");

            var events = _eventStoreConnection.ReadAllEventsForwardAsync(position, _blockSize, false).Result;

            if (!_subscriptionOnline && events.Events.Count() != _blockSize)
            {
                _subscriptionOnline = true;
                InitializeSubscription(events.NextPosition);
            }

            //Console.WriteLine($"{DateTime.Now:yyyy-MM-dd hh:mm:ss}: GetFromPosition(long position = {position}), next: {events.NextPosition}");
            return new EventStream
            {
                Events = events.Events.Select(e => new Event() { Position = e.OriginalPosition.Value }).ToList(),
                NextPosition = events.NextPosition
            };
        }

        private void InitializeSubscription(Position position)
        {
            //Console.WriteLine($"SubscribeToAllFrom({position})");
            var settings = new CatchUpSubscriptionSettings(_blockSize, _blockSize, false, false);

            _eventStoreConnection.SubscribeToAllFrom(position, settings, EventAppeared);
        }

        private void EventAppeared(EventStoreCatchUpSubscription eventStoreSubscription, ResolvedEvent resolvedEvent)
        {
            //Console.WriteLine($"EventAppeared Position: {resolvedEvent.OriginalPosition}");
            _eventReceivedTrigger();
            //Console.WriteLine($"PosX: {resolvedEvent.OriginalPosition}, Type: {resolvedEvent.Event.EventType}");
        }        
    }
}