using ESPlus.EventHandlers;
using ESPlus.Subscribers;

namespace ESPlus.FlushPolicies
{
    public class AlwaysFlushPolicy : IFlushPolicy
    {
        public IEventHandler EventHandler { get; set; }

        public void FlushOnEvent()
        {
            EventHandler.Flush();
        }

        public void FlushWhenAhead()
        {
        }
    }    
}