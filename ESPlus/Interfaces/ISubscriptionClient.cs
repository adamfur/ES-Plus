using System.Collections.Generic;
using System.Threading;

namespace ESPlus.Subscribers
{
    public interface ISubscriptionClient
    {
        SubscriptionContext SubscriptionContext { get; }
        public IAsyncEnumerable<Event> Events(CancellationToken cancellationToken = default);
    }
}