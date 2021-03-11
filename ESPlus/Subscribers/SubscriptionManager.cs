using System.Collections.Generic;
using ESPlus.Misc;
using ESPlus.Wyrm;

namespace ESPlus.Subscribers
{
    public class SubscriptionManager : ISubscriptionManager
    {
        private readonly IWyrmDriver _driver;
        private readonly IEventTypeResolver _eventTypeResolver;

        public SubscriptionManager(IWyrmDriver driver, IEventTypeResolver eventTypeResolver)
        {
            _driver = driver;
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
                
            return new WyrmSubscriptionClient(context, _driver, _eventTypeResolver);
        }
    }
}
