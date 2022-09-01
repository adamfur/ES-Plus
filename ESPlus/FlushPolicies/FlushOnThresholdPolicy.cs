using System.Threading;
using System.Threading.Tasks;
using ESPlus.EventHandlers;
using ESPlus.Subscribers;

namespace ESPlus.FlushPolicies
{
    public class FlushOnThresholdPolicy : IFlushPolicy
    {
        private const int EventThreshold = 1_000;
        private int _events = 0;

        public void ScheduleFlush()
        {
            _events = EventThreshold;
        }

        public IEventHandler EventHandler { get; set; }

        public virtual Task FlushWhenAheadAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public async Task FlushOnEventAsync(CancellationToken cancellationToken)
        {
            if (++_events >= EventThreshold)
            {
                await FlushAsync(cancellationToken);
            }
        }

        protected async Task FlushAsync(CancellationToken cancellationToken)
        {
            await EventHandler.FlushAsync(cancellationToken);
            _events = 0;
        }
    }
}