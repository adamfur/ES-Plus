using System.Threading;
using System.Threading.Tasks;
using ESPlus.EventHandlers;

namespace ESPlus.Subscribers
{
    // Event, Batch, IncompleteBatch, Time
    public interface IFlushPolicy
    {
        Task FlushWhenAheadAsync(CancellationToken cancellationToken);
        Task FlushOnEventAsync(CancellationToken cancellationToken);
        void ScheduleFlush();
        IEventHandler EventHandler { get; set; }
    }
}