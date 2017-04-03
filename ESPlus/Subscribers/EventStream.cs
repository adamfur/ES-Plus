using System.Collections.Generic;

namespace ESPlus.Subscribers
{
    public class EventStream
    {
        public List<Event> Events { get; set; }
        public bool IsEndOfStream { get; set; }
        public long NextPosition { get; set; }
    }
}