using System.Collections;
using System.Collections.Generic;
using System.Threading;
using ESPlus.Misc;
using ESPlus.Subscribers;

namespace ESPlus.Wyrm
{
    public class WyrmSubscriptionClient : ISubscriptionClient
    {
        private readonly SubscriptionContext _subscriptionContext;
        private readonly IWyrmDriver _wyrmConnection;
        private readonly IEventTypeResolver _eventTypeResolver;

        public WyrmSubscriptionClient(SubscriptionContext subscriptionContext, IWyrmDriver wyrmConnection, IEventTypeResolver eventTypeResolver)
        {
            _subscriptionContext = subscriptionContext;
            _wyrmConnection = wyrmConnection;
            _eventTypeResolver = eventTypeResolver;
        }

        public async IAsyncEnumerator<Event> GetEnumerator()
        {
            await foreach (var @event in _wyrmConnection.ReadFrom(_subscriptionContext.Position).Subscribe().QueryEventsAsync())
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

        public IAsyncEnumerator<Event> GetAsyncEnumerator(CancellationToken cancellationToken = new CancellationToken())
        {
            return GetEnumerator();
        }
    }
}