using System.Threading.Tasks;
using ESPlus.EventHandlers;
using ESPlus.Subscribers;

namespace ESPlus.FlushPolicies
{
    public class FlushOnThresholdPolicy : IFlushPolicy
    {
        private const int EventThreshold = 100;
        private int _events = 0;

        public IEventHandler EventHandler { get; set; }

        public virtual Task FlushWhenAheadAsync()
        {
            return Task.CompletedTask;
        }

        public async Task FlushOnEventAsync()
        {
            if (++_events > EventThreshold)
            {
                await FlushAsync();
            }
        }

        protected async Task FlushAsync()
        {
            await EventHandler.FlushAsync();
            _events = 0;
        }
    }
}