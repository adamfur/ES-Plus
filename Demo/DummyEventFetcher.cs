using System;
using System.Collections.Generic;

namespace ESPlus.Subscribers
{
    public class DummyEventFetcher : IEventFetcher
    {
        public IEnumerable<Event> GetFromPosition(long position)
        {
            Console.WriteLine($"Request: {position}");

            var result = new List<Event>();

            for (var i = position + 1; i < position + 512; ++i)
            {
                result.Add(new Event { Position = i });
            }
            return result;
        }
    }
}