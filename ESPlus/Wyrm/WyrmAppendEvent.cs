using System;

namespace ESPlus.Wyrm
{
    public class WyrmAppendEvent
    {
        public WyrmAppendEvent(Guid eventId, string eventType, byte[] body, byte[] metadata, string streamName, long version)
        {
            EventId = eventId;
            EventType = eventType;
            Body = body;
            Metadata = metadata;
            StreamName = streamName;
            Version = version;
        }

        public Guid EventId { get; }
        public string EventType { get; }
        public byte[] Body { get; }
        public byte[] Metadata { get; }
        public string StreamName { get; }
        public long Version { get; }
    }
}