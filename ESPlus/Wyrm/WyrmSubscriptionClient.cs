using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using ESPlus.Misc;
using ESPlus.Subscribers;

namespace ESPlus.Wyrm
{
    public class WyrmSubscriptionClient : ISubscriptionClient
    {
        public SubscriptionContext SubscriptionContext { get; }
        private readonly IWyrmDriver _wyrmConnection;
        private readonly IEventTypeResolver _eventTypeResolver;

        public WyrmSubscriptionClient(SubscriptionContext subscriptionContext, IWyrmDriver wyrmConnection, IEventTypeResolver eventTypeResolver)
        {
            SubscriptionContext = subscriptionContext;
            _wyrmConnection = wyrmConnection;
            _eventTypeResolver = eventTypeResolver;
        }
        
        public async IAsyncEnumerable<Event> Events([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (SubscriptionContext.Position.Equals(Position.Start))
            {
                yield return new Event(_eventTypeResolver, null)
                {
                    InitEvent = true,
                    Position = Position.Start,
                };
            }
            
            await foreach (var @event in _wyrmConnection.SubscribeAsync(SubscriptionContext.Position, cancellationToken))
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
                    DeleteEvent = @event.DeleteEvent,
                    TimestampUtc = @event.TimestampUtc,
                };
            }
        }
    }
}