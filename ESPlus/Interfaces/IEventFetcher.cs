using System.Collections.Generic;

namespace ESPlus.Subscribers
{
    public interface IEventFetcher
    {
        IEnumerable<Event> GetFromPosition(long position);
    }
}