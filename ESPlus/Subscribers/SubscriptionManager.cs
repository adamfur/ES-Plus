using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ESPlus.Misc;
using ESPlus.Wyrm;

namespace ESPlus.Subscribers
{
    public class SubscriptionManager : ISubscriptionManager
    {
        private readonly List<SubscriptionContext> _contexts = new List<SubscriptionContext>();
        private object _mutex = new object();
        private readonly WyrmDriver _wyrmConnection;
        private readonly IEventTypeResolver _eventTypeResolver;
        private readonly IEventSerializer _eventSerializer;

        public SubscriptionManager(WyrmDriver wyrmConnection, IEventTypeResolver eventTypeResolver, IEventSerializer eventSerializer)
        {
            _wyrmConnection = wyrmConnection;
            _eventTypeResolver = eventTypeResolver;
            _eventSerializer = eventSerializer;
        }

        public ISubscriptionClient Subscribe(byte[] position, CancellationToken cancellationToken)
        {
            var context = new SubscriptionContext
            {
                Position = position,
                Manager = this,
                Future = position,
                CancellationToken = cancellationToken
            };

            lock (_mutex)
            {
                _contexts.Add(context);
                Monitor.Pulse(_mutex);
            }

            return new WyrmSubscriptionClient(context, _wyrmConnection, _eventTypeResolver);
        }
    }
}
