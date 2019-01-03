using System;

namespace ESPlus.Wyrm
{
    public class WyrmEvent2
    {
        public long Offset { get; set; }
        public long TotalOffset { get; set; }
        public Guid EventId { get; set; }
        public long Version { get; set; }
        public DateTime Timestamp { get; set; }
        public byte[] Metadata { get; set; }
        public byte[] Data { get; set; }
        public string EventType { get; set; }
        public string StreamName { get; set; }
        public byte[] Position { get; set; }
        public IEventSerializer Serializer { get; set; }
        public bool IsAhead { get; set; }
    }
}
