using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ESPlus.Misc;
using EventStore.ClientAPI;

namespace ESPlus.Subscribers
{
    public class EventFetcher : IEventFetcher
    {
        private readonly IEventStoreConnection _eventStoreConnection;
        private readonly IEventTypeResolver _eventTypeResolver;
        private readonly int _blockSize;
        private bool _subscriptionOnline = false;
        private Action _eventReceivedTrigger = () => { };

        public EventFetcher(IEventStoreConnection eventStoreConnection, IEventTypeResolver eventTypeResolver, int blockSize = 512)
        {
            _eventStoreConnection = eventStoreConnection;
            _eventTypeResolver = eventTypeResolver;
            _blockSize = blockSize;
        }

        public void OnEventReceived(Action action)
        {
            _eventReceivedTrigger = action;
        }

        public EventStream GetFromPosition(Position position)
        {
            // return new EventStream
            // {
            //     Events = Enumerable.Range(0, _blockSize)
            //         .Select(x => new Event
            //         {
            //             Position = Position.End,
            //             EventType = "Dummy"
            //         }).ToList(),
            //     NextPosition = Position.End
            // };

            var events = _eventStoreConnection.ReadAllEventsForwardAsync(position, _blockSize, false).Result;

            if (!_subscriptionOnline && events.Events.Count() != _blockSize)
            {
                _subscriptionOnline = true;
                InitializeSubscription(events.NextPosition);
            }

            //Console.WriteLine($"{DateTime.Now:yyyy-MM-dd hh:mm:ss}: GetFromPosition(long position = {position}), next: {events.NextPosition}");
            return new EventStream
            {
                Events = events.Events.Select(e => new Event(_eventTypeResolver)
                {
                    Position = e.OriginalPosition.Value,
                    Payload = e.Event.Data,
                    Meta = e.Event.Metadata,
                    EventType = e.Event.EventType
                }).ToList(),
                NextPosition = events.NextPosition
            };
        }

        private void InitializeSubscription(Position position)
        {
            //Console.WriteLine($"SubscribeToAllFrom({position})");
            var settings = new CatchUpSubscriptionSettings(_blockSize, _blockSize, false, false);

            _eventStoreConnection.SubscribeToAllFrom(position, settings, EventAppeared, LiveProcessingStarted, SubscriptionDropReason);
        }

        private void LiveProcessingStarted(EventStoreCatchUpSubscription obj)
        {
            Console.WriteLine("LiveProcessingStarted");
        }

        private void SubscriptionDropReason(EventStoreCatchUpSubscription arg1, SubscriptionDropReason arg2, Exception arg3)
        {
            _subscriptionOnline = false;
            Console.WriteLine($"SubscriptionDropReason: {arg2.ToString()}");
        }

        private Task EventAppeared(EventStoreCatchUpSubscription arg1, ResolvedEvent arg2)
        {
            Console.WriteLine("EventAppeared");
            //Console.WriteLine($"EventAppeared Position: {resolvedEvent.OriginalPosition}");
            _eventReceivedTrigger();
            //Console.WriteLine($"PosX: {resolvedEvent.OriginalPosition}, Type: {resolvedEvent.Event.EventType}");
            return Task.WhenAll();
        }
    }
}