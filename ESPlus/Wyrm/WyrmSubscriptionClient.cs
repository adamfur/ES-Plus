using System;
using System.Collections;
using System.Collections.Generic;
using ESPlus.Misc;
using ESPlus.Subscribers;

namespace ESPlus.Wyrm
{
    public class WyrmSubscriptionClient : ISubscriptionClient
    {
        private SubscriptionContext _subscriptionContext;
        private readonly WyrmConnection _wyrmConnection;
        private readonly IEventTypeResolver _eventTypeResolver;
        private readonly IEventSerializer _eventSerializer;

        public WyrmSubscriptionClient(SubscriptionContext subscriptionContext, WyrmConnection wyrmConnection, IEventTypeResolver eventTypeResolver, IEventSerializer eventSerializer)
        {
            _subscriptionContext = subscriptionContext;
            _wyrmConnection = wyrmConnection;
            _eventTypeResolver = eventTypeResolver;
            _eventSerializer = eventSerializer;
        }

        public IEnumerator<Event> GetEnumerator()
        {
            foreach (var @event in _wyrmConnection.EnumerateAll(_subscriptionContext.Position))
            {
                yield return new Event(_eventTypeResolver, _eventSerializer)
                {
                    Position = @event.Position,
                    Meta = @event.Metadata,
                    Payload = @event.Data,
                    EventType = @event.EventType,
                    IsAhead = false
                };

                if (@event.Offset == @event.TotalOffset)
                {
                    yield return new Event(_eventTypeResolver, _eventSerializer)
                    {
                        IsAhead = true
                    };
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}