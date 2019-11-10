using System;

namespace ESPlus.Wyrm
{
    public abstract class WyrmItem
    {
        public abstract void Accept(IWyrmItemVisitor visitor);
    }

    public class WyrmEventItem : WyrmItem
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
        public string CreateEvent { get; set; }
        
        public override void Accept(IWyrmItemVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public class WyrmAheadItem : WyrmItem
    {
        public override void Accept(IWyrmItemVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
    
    public class WyrmVersionItem : WyrmItem
    {
        public long StreamVersion { get; set; }
        
        public override void Accept(IWyrmItemVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public interface IWyrmItemVisitor
    {
        void Visit(WyrmEventItem item);
        void Visit(WyrmAheadItem item);
        void Visit(WyrmVersionItem item);
    }
}
