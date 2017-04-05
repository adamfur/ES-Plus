using System.Collections.Generic;
using System.Linq;
using EventStore.ClientAPI;

namespace ESPlus.Subscribers
{
    public class EventFetcherCacheRow
    {
        private EventStream _events;
        public Position From { get; set; }
        public Position To { get; set; }

        public EventFetcherCacheRow(Position from, Position to, EventStream events)
        {
            From = from;
            To = to;
            _events = events;
        }

        public bool Within(Position position)
        {
            return From <= position && position <= To;
        }
/*
        public void Merge(IEnumerable<Event> events)
        {
            _events = _events.Concat(events)
                .DistinctBy(x => x.Position)
                .OrderBy(x => x.Position)
                .ToList();

            To = _events.Last().Position;
        }
*/
        public EventStream Select(Position position)
        {
            return new EventStream 
            {
                Events = _events.Events.Where(e => e.Position >= position).ToList(),
                NextPosition = _events.NextPosition
            };
        }
    }
}