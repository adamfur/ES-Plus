using System.Collections.Generic;
using System.Linq;

namespace ESPlus.Subscribers
{
    public class CachedEventFetcher : IEventFetcher
    {
        private readonly IEventFetcher _concrete;
        private readonly LinkedList<EventFetcherCacheRow> _cache = new LinkedList<EventFetcherCacheRow>();
        private int _cachedItems = 0;
        private const int CacheLimit = 40960;

        public CachedEventFetcher(IEventFetcher eventFetcher)
        {
            _concrete = eventFetcher;
        }

        public IEnumerable<Event> GetFromPosition(long position)
        {
            foreach (var row in _cache)
            {
                if (row.Within(position))
                {
                    return row.Select(position);
                }
            }

            var data = _concrete.GetFromPosition(position).ToList();
            
            _cachedItems += data.Count;
            _cache.AddFirst(new EventFetcherCacheRow(position, data.Last().Position, data));

            while (_cachedItems > CacheLimit)
            {
                var items = _cache.Last().Select(0).Count();

                _cache.RemoveLast();
                _cachedItems -= items;
            }

            return data;
        }
    }
}