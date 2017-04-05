using System.Collections.Generic;
using EventStore.ClientAPI;

namespace ESPlus.Subscribers
{
    public class EventStream
    {
        public List<Event> Events { get; set; } = new List<Event>();
        public Position NextPosition { get; set; }
    }
}