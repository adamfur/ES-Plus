using System.Collections.Generic;

namespace ESPlus.Subscribers
{
    public interface ISubscriptionClient : IEnumerable<Event>
    {
        Priority Priority { get; set; }
    }
}