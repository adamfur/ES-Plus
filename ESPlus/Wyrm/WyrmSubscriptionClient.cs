using System.Collections;
using System.Collections.Generic;
using ESPlus.Misc;
using ESPlus.Subscribers;

namespace ESPlus.Wyrm
{
    public class WyrmSubscriptionClient : ISubscriptionClient
    {
        private SubscriptionContext _subscriptionContext;
        private readonly WyrmDriver _wyrmConnection;
        private readonly IEventTypeResolver _eventTypeResolver;

        public WyrmSubscriptionClient(SubscriptionContext subscriptionContext, WyrmDriver wyrmConnection, IEventTypeResolver eventTypeResolver)
        {
            _subscriptionContext = subscriptionContext;
            _wyrmConnection = wyrmConnection;
            _eventTypeResolver = eventTypeResolver;
        }

        public IEnumerator<Event> GetEnumerator()
        {
            foreach (var @event in _wyrmConnection.Subscribe(_subscriptionContext.Position))
            {
                yield return new Event(_eventTypeResolver, @event.Serializer)
                {
                    Position = new Position(@event.Position),
                    Meta = @event.Metadata,
                    Payload = @event.Data,
                    EventType = @event.EventType,
                    IsAhead = @event.IsAhead,
                    StreamName = @event.StreamName,
                    Offset = @event.Offset,
                    TotalOffset = @event.TotalOffset,
                };
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}