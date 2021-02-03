using System.Threading.Tasks;
using ESPlus.EventHandlers;
using ESPlus.Subscribers;

namespace ESPlus.FlushPolicies
{
    public class AlwaysFlushPolicy : IFlushPolicy
    {
        public IEventHandler EventHandler { get; set; }

        public async Task FlushOnEventAsync()
        {
            await EventHandler.FlushAsync();
        }

        public Task FlushWhenAheadAsync()
        {
            return Task.CompletedTask;
        }
    }    
}