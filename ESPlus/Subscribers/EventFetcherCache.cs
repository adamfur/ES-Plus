using System.Collections.Generic;

namespace ESPlus.Subscribers
{
    public class EventFetcherCache : IEventFetcher
    {
        private IEventFetcher _concrete;

        public EventFetcherCache(IEventFetcher concrete)
        {
            _concrete = concrete;
        }

        public IEnumerable<object> GetFromPosition(long position)
        {
            return _concrete.GetFromPosition(position);
        }
    }
}