namespace ESPlus.Subscribers
{
    public class Event
    {
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

        public Event(IEventSerializer eventSerializer)
        {
            _eventSerializer = eventSerializer;
        }

        public object DeserializedItem()
        {
            return _eventSerializer.Deserialize(EventType, Payload);
        }
    }
}