using System.Text;
using ESPlus.Misc;
using Newtonsoft.Json;

namespace ESPlus.Subscribers
{
    public class Event
    {
        private readonly IEventTypeResolver _eventTypeResolver;
        public Position Position { get; set; }
        public byte[] Meta { get; set; }
        public byte[] Payload { get; set; }
        public string EventType { get; set; }
        public bool IsAhead { get; set; } = false;

        public Event(IEventTypeResolver eventTypeResolver)
        {
            _eventTypeResolver = eventTypeResolver;
        }

        public Event(bool isAhead)
        {
            IsAhead = isAhead;
        }

        public object DeserializedItem()
        {
            var json = Encoding.UTF8.GetString(Payload);
            var result = JsonConvert.DeserializeObject(json, _eventTypeResolver.ResolveType(EventType));

            return result;
        }
    }
}