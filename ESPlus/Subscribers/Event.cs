using System.Text;
using ESPlus.Misc;
using Newtonsoft.Json;

namespace ESPlus.Subscribers
{
    public class Event
    {
        private readonly IEventTypeResolver _eventTypeResolver;
        private object _cache;
        private object _mutex = new object();

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
            if (_cache != null)
            {
                return _cache;
            }

            lock (_mutex)
            {
                if (_cache != null)
                {
                    return _cache;
                }
                
                var json = Encoding.UTF8.GetString(Payload);
                var result = JsonConvert.DeserializeObject(json, _eventTypeResolver.ResolveType(EventType));

                _cache = result;
            }
            return _cache;
        }


        public Position Position { get; set; }
        public byte[] Meta { get; set; }
        public byte[] Payload { get; set; }
        public string EventType { get; set; }
        public bool IsAhead { get; set; } = false;
    }
}