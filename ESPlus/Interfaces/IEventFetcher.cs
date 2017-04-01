using System.Collections.Generic;

namespace ESPlus.Subscribers
{
    public interface IEventFetcher
    {
        IEnumerable<object> GetFromPosition(long position);
    }
}