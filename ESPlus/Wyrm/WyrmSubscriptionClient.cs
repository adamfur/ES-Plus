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
        
        public async IAsyncEnumerator<Event> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            await foreach (var @event in _wyrmConnection.SubscribeAsync(_subscriptionContext.Position, cancellationToken))
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
                    CreateEvent = @event.CreateEvent,
                    TimestampUtc = @event.TimestampUtc,
                };
            }
        }
    }
}