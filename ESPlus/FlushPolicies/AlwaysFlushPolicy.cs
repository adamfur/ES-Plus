using System.Threading;
using System.Threading.Tasks;
using ESPlus.EventHandlers;
using ESPlus.Subscribers;

namespace ESPlus.FlushPolicies
{
    public class AlwaysFlushPolicy : IFlushPolicy
    {
        public void ScheduleFlush()
        {
        }

        public IEventHandler EventHandler { get; set; }

        public async Task FlushOnEventAsync(CancellationToken cancellationToken)
        {
            await EventHandler.FlushAsync(cancellationToken);
        }

        public Task FlushWhenAheadAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }    
}