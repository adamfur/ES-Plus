using System.Collections.Generic;
using System.Linq;

namespace ESPlus.Subscribers
{
    public class EventFetcherCacheRow
    {
        private List<Event> _events = new List<Event>();
        public long From { get; set; }
        public long To { get; set; }

        public EventFetcherCacheRow(long from, long to, IEnumerable<Event> events)
        {
            From = from;
            To = to;
            _events = new List<Event>(events);
        }

        public bool Within(long position)
        {
            return From <= position && position <= To;
        }

        public void Merge(IEnumerable<Event> events)
        {
            _events = _events.Concat(events)
                .DistinctBy(x => x.Position)
                .OrderBy(x => x.Position)
                .ToList();

            To = _events.Last().Position;
        }

        public IEnumerable<Event> Select(long position)
        {
            return _events.Where(e => e.Position >= position); // CORRECT?
        }
    }
}