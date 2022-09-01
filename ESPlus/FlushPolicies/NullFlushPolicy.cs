using System.Threading;
using System.Threading.Tasks;
using ESPlus.EventHandlers;
using ESPlus.Subscribers;

namespace ESPlus.FlushPolicies
{
    public class NullFlushPolicy : IFlushPolicy
    {
        public void ScheduleFlush()
        {
        }

        public IEventHandler EventHandler { get; set; }

        public Task FlushOnEventAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task FlushWhenAheadAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}