using System.Collections;
using System.Collections.Generic;
using ESPlus.Misc;
using ESPlus.Subscribers;

namespace ESPlus.Wyrm
{
    public class WyrmSubscriptionClient : ISubscriptionClient
    {
        private SubscriptionContext _subscriptionContext;
        private readonly IWyrmDriver _wyrmConnection;
        private readonly IEventTypeResolver _eventTypeResolver;

        public WyrmSubscriptionClient(SubscriptionContext subscriptionContext, IWyrmDriver wyrmConnection, IEventTypeResolver eventTypeResolver)
        {
            _subscriptionContext = subscriptionContext;
            _wyrmConnection = wyrmConnection;
            _eventTypeResolver = eventTypeResolver;
        }

        public IEnumerator<Event> GetEnumerator()
        {
            foreach (var @event in _wyrmConnection.SubscribeAll(_subscriptionContext.Position))
            {
                if (@event is WyrmEventItem evt)
                {
                    yield return new Event(_eventTypeResolver, evt.Serializer)
                    {
                        Position = new Position(evt.Position),
                        Meta = evt.Metadata,
                        Payload = evt.Data,
                        EventType = evt.EventType,
                        IsAhead = evt.IsAhead,
                        StreamName = evt.StreamName,
                        Offset = evt.Offset,
                        TotalOffset = evt.TotalOffset,
                        CreateEvent = evt.CreateEvent,
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