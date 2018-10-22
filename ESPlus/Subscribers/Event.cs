using System.Text;
using ESPlus.Misc;
using Newtonsoft.Json;

namespace ESPlus.Subscribers
{
    public class Event
    {
        private readonly IEventTypeResolver _eventTypeResolver;
        private readonly IEventSerializer _eventSerializer;

        public byte[] Position { get; set; }
        public byte[] Meta { get; set; }
        public byte[] Payload { get; set; }
        public string EventType { get; set; }
        public bool IsAhead { get; set; } = false;

        public Event(IEventTypeResolver eventTypeResolver, IEventSerializer eventSerializer)
        {
            _eventTypeResolver = eventTypeResolver;
            _eventSerializer = eventSerializer;
        }

        public Event(bool isAhead)
        {
            IsAhead = isAhead;
        }

        public object DeserializedItem()
        {
            var type = _eventTypeResolver.ResolveType(EventType);
            
            return _eventSerializer.Deserialize(type, Payload);
        }
    }
}