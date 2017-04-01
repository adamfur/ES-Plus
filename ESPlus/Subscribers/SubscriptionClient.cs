using System.Collections;
using System.Collections.Generic;

namespace ESPlus.Subscribers
{
    public class SubscriptionClient : ISubscriptionClient
    {
        private SubscriptionContext _subscriptionContext;

        public SubscriptionClient(SubscriptionContext subscriptionContext)
        {
            _subscriptionContext = subscriptionContext;
        }

        public IEnumerator<Event> GetEnumerator()
        {
            while (true)
            {
                var list = _subscriptionContext.Take();

                foreach (var @event in list)
                {
                    yield return @event;
                    _subscriptionContext.Position = @event.Position;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}