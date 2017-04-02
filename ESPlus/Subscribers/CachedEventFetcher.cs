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
        private object _mutex = new object();

        public CachedEventFetcher(IEventFetcher eventFetcher)
        {
            _concrete = eventFetcher;
        }

        private IEnumerable<Event> GetFromCache(long position)
        {
            foreach (var row in _cache)
            {
                if (row.Within(position + 1))
                {
                    var events = row.Select(position).ToList();

                    if (events.Count != 0)
                    {
                        return events;
                    }
                    return new List<Event>();
                }
            }

            return new List<Event>();
        }

        public IEnumerable<Event> GetFromPosition(long position)
        {
            lock (_mutex)
            {
                var events = GetFromCache(position);

                if (events != null)
                {
                    return events;
                }
            }

            List<Event> data;

            lock (position.ToString())
            {
                lock (_mutex)
                {
                    var events = GetFromCache(position);

                    if (events != null)
                    {
                        return events;
                    }
                }
                data = _concrete.GetFromPosition(position).ToList();
            }

            lock (_mutex)
            {
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
}