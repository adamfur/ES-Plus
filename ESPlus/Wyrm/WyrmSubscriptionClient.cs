using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
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
            foreach (var @event in _wyrmConnection.EnumerateAll(_subscriptionContext.Position))
            {
                yield return new Event(_eventTypeResolver, @event.Serializer)
                {
                    Position = @event.Position,
                    Meta = @event.Metadata,
                    Payload = @event.Data,
                    EventType = @event.EventType,
                    IsAhead = @event.IsAhead
                };
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}