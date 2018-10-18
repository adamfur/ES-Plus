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
        private IEventFetcher _eventFetcher;
        private readonly WyrmConnection _wyrmConnection;
        private readonly IEventTypeResolver _eventTypeResolver;

        public SubscriptionManager(WyrmConnection wyrmConnection, IEventTypeResolver eventTypeResolver)
        {
            this._wyrmConnection = wyrmConnection;
            this._eventTypeResolver = eventTypeResolver;
        }

        public void Start()
        {
        }

        public ISubscriptionClient Subscribe(Position position, CancellationToken cancellationToken)
        {
            var context = new SubscriptionContext
            {
                Position = position,
                RequestStatus = RequestStatus.Initialized,
                Manager = this,
                Future = position,
                SynchronizedAction = a => {},
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
