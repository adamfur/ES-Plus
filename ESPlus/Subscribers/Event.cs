using EventStore.ClientAPI;

namespace ESPlus.Subscribers
{
    public class Event
    {
        public Position Position { get; set; }
        public byte[] Meta { get; set; }
        public byte[] Payload { get; set; }
        public string EventType { get; internal set; }
    }
}