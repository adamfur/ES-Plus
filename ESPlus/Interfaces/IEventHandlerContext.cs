using System;
using System.Threading;
using System.Threading.Tasks;

namespace ESPlus.EventHandlers
{
    public interface IEventHandlerContext
    {
        DateTime TimestampUtc { get; set; }
        Position Checkpoint { get; set; }
        long Offset { get; set; }
        long TotalOffset { get; set; }
        string StreamName { get; set; }
        MetaData Metadata { get; set; }
        Task FlushAsync(CancellationToken cancellationToken);
        Task EvictCache();
    }
}
