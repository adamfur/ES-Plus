using System;
using System.Collections.Generic;
using System.Linq;

namespace ESPlus.Wyrm
{
    public enum CommitPolicy
    {
        All = 0,
        Any = 1,
    }

    public enum BundleOp
    {
        Create = 1,
        Delete = 2,
        Events = 3,
    }

    public class Bundle
    {
        public CommitPolicy Policy { get; set; }
        public List<BundleItem> Items { get; set; } = new List<BundleItem>();
    }

    public abstract class BundleItem
    {
        public abstract int Count();

        public abstract BundleType Type { get; }
        public string StreamName { get; set; }
    }

    public class CreateBundleItem : BundleItem
    {
        public override int Count()
        {
            return 8 + StreamName.Length;
        }

        public override BundleType Type => BundleType.Create;
    }

    public class DeleteBundleItem : BundleItem
    {
        public override int Count()
        {
            return 16 + StreamName.Length;
        }

        public override BundleType Type => BundleType.Delete;

        public long StreamVersion { get; set; } = -1;
    }

    public class EventsBundleItem : BundleItem
    {
        public override int Count()
        {
            return 21 + StreamName.Length + Events.Sum(x => x.Count());
        }

        public override BundleType Type => BundleType.Events;

        public long StreamVersion { get; set; } = 0;
        public List<BundleEvent> Events { get; set; } = new List<BundleEvent>();
        public bool Encrypt { get; set; } = true;
    }

    public class BundleEvent
    {
        public int Count()
        {
            return 28 + EventType.Length + Metadata.Length + Body.Length;
        }

        public Guid EventId { get; set; }
        public string EventType { get; set; }
        public byte[] Metadata { get; set; }
        public byte[] Body { get; set; }
    }

    public enum BundleType
    {
        Create = 1,
        Delete = 2,
        Events = 3,
    }
}