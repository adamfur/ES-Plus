using EventStore.ClientAPI;

namespace ESPlus.Subscribers
{
    public class Event
    {
        public Position Position { get; set; }
        public string Meta { get; set; }
        public string Payload { get; set; }
    }
}