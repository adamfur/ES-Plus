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
                    _subscriptionContext.Position = @event.Position;
                    yield return @event;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}