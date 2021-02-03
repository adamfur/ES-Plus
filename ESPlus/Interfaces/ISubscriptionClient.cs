using System.Collections.Generic;
using System.Threading;

namespace ESPlus.Subscribers
{
    public interface ISubscriptionClient
    {
        public IAsyncEnumerable<Event> Events(CancellationToken cancellationToken = default);
    }
}