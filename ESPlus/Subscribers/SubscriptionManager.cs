using System.Collections.Generic;
using System.Threading;
using ESPlus.Misc;
using ESPlus.Wyrm;

namespace ESPlus.Subscribers
{
    public class SubscriptionManager : ISubscriptionManager
    {
        private readonly List<SubscriptionContext> _contexts = new List<SubscriptionContext>();
        private readonly IWyrmDriver _wyrmConnection;
        private readonly IEventTypeResolver _eventTypeResolver;

        public SubscriptionManager(IWyrmDriver wyrmConnection, IEventTypeResolver eventTypeResolver)
        {
            _wyrmConnection = wyrmConnection;
            _eventTypeResolver = eventTypeResolver;
        }

        public ISubscriptionClient Subscribe(Position position)
        {
            var context = new SubscriptionContext
            {
                Position = position,
                Manager = this,
                Future = position
            };
            
            _contexts.Add(context);
                
            return new WyrmSubscriptionClient(context, _wyrmConnection, _eventTypeResolver);
        }
    }
}
