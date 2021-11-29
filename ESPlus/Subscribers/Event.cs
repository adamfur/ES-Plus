using System;
using ESPlus.Misc;

namespace ESPlus.Subscribers
{
    public class Event
    {
        private readonly IEventTypeResolver _eventTypeResolver;
        private readonly IEventSerializer _eventSerializer;

        public Position Position { get; set; }
        public byte[] Meta { get; set; }
        public byte[] Payload { get; set; }
        public string EventType { get; set; }
        public bool IsAhead { get; set; }
        public string StreamName { get; set; }
        public long Offset { get; set; }
        public long TotalOffset { get; set; }
        public string CreateEvent { get; set; }
        public DateTime TimestampUtc { get; set; }
        public bool InitEvent { get; set; }

        public Event(IEventTypeResolver eventTypeResolver, IEventSerializer eventSerializer)
        {
            _eventTypeResolver = eventTypeResolver;
            _eventSerializer = eventSerializer;
        }

        public object DeserializedItem()
        {
            var type = _eventTypeResolver.ResolveType(EventType);
            
            return _eventSerializer.Deserialize(type, Payload);
        }
    }
}