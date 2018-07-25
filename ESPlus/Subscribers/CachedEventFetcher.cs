using System;
using System.Collections.Generic;
using System.Linq;

namespace ESPlus.Subscribers
{
    public class CachedEventFetcher : IEventFetcher
    {
        private readonly IEventFetcher _concrete;
        private readonly LinkedList<EventFetcherCacheRow> _cache = new LinkedList<EventFetcherCacheRow>();
        private int _cachedItems = 0;
        private const int CacheLimit = 4096;
        private object _mutex = new object();

        public CachedEventFetcher(IEventFetcher eventFetcher)
        {
            _concrete = eventFetcher;
        }

        public EventStream GetFromPosition(Position position)
        {
            lock (_mutex)
            {
                var events = GetFromCache(position);

                if (events.Events.Any())
                {
                    //Console.WriteLine($"{DateTime.Now:yyyy-MM-dd hh:mm:ss}: GetFromCache(long position = {position.CommitPosition}), next: {events.NextPosition}");
                    return events;
                }
            }

            EventStream data;

            lock (string.Intern(position.ToString()))
            {
                lock (_mutex)
                {
                    var stream = GetFromCache(position);

                    if (stream.Events.Any())
                    {
                        //Console.WriteLine($"{DateTime.Now:yyyy-MM-dd hh:mm:ss}: GetFromCache(long position = {position.CommitPosition}), next: {stream.NextPosition}");
                        return stream;
                    }
                }

                data = _concrete.GetFromPosition(position);

                if (!data.Events.Any())
                {
                    //Console.WriteLine($"{DateTime.Now:yyyy-MM-dd hh:mm:ss}: GetFromCache(long position = {position.CommitPosition}), next: {data.NextPosition}");
                    return new EventStream
                    {
                        NextPosition = position
                    };
                }
            }

            lock (_mutex)
            {
                AddToCache(position, data);
                ExpireCache();
                //Console.WriteLine($"{DateTime.Now:yyyy-MM-dd hh:mm:ss}: GetFromCache(long position = {position.CommitPosition}), next: {data.NextPosition}");
                return data;
            }
        }

        private EventStream GetFromCache(Position position)
        {
            foreach (var row in _cache)
            {
                if (row.Within(position))
                {
                    var stream = row.Select(position);

                    //Console.WriteLine($"{DateTime.Now:yyyy-MM-dd hh:mm:ss}: InCache(long position = {position.CommitPosition}) ={stream.Events.Any()}");
                    if (stream.Events.Any())
                    {
                        return stream;
                    }

                    return new EventStream
                    {
                        NextPosition = position
                    };
                }
            }

            return new EventStream
            {
                NextPosition = position
            };
        }

        private void ExpireCache()
        {
            while (_cachedItems > CacheLimit)
            {
                var itemCount = _cache.Last().Select(Position.Start).Events.Count();

                _cache.RemoveLast();
                _cachedItems -= itemCount;
            }
        }

        private void AddToCache(Position position, EventStream stream)
        {
            //Console.WriteLine($"{DateTime.Now:yyyy-MM-dd hh:mm:ss}: AddToCache(long position = {position.CommitPosition})");
            _cachedItems += stream.Events.Count;
            _cache.AddFirst(new EventFetcherCacheRow(position, stream.NextPosition, stream));
        }

        public void OnEventReceived(Action action)
        {
            _concrete.OnEventReceived(action);
        }
    }
}