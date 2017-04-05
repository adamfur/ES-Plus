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
                var stream = _subscriptionContext.Take();

                foreach (var @event in stream.Events)
                {
                    _subscriptionContext.Position = @event.Position;
                    yield return @event;
                }
                _subscriptionContext.Position = stream.NextPosition;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}