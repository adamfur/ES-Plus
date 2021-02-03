using System.Threading.Tasks;
using ESPlus.EventHandlers;
using ESPlus.Subscribers;

namespace ESPlus.FlushPolicies
{
    public class NullFlushPolicy : IFlushPolicy
    {
        public IEventHandler EventHandler { get; set; }

        public Task FlushOnEventAsync()
        {
            return Task.CompletedTask;
        }

        public Task FlushWhenAheadAsync()
        {
            return Task.CompletedTask;
        }
    }
}