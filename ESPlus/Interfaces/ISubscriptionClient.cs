using System.Collections.Generic;

namespace ESPlus.Subscribers
{
    public interface ISubscriptionClient : IAsyncEnumerable<Event>
    {
    }
}