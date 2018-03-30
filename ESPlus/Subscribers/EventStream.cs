using System.Collections.Generic;
using EventStore.ClientAPI;

namespace ESPlus.Subscribers
{
    public class EventStream
    {
        public static EventStream Ahead = new EventStream
        {
            Events = new List<Event>
            {
                new Event(isAhead: true),
            },
            IsArtificial = true
        };
        public List<Event> Events { get; set; } = new List<Event>();
        public Position NextPosition { get; set; }
        public bool IsArtificial { get; set; } = false;
    }
}