using ESPlus.EventHandlers;
using ESPlus.Subscribers;

namespace ESPlus
{
    public class AlwaysFlushPolicy : IFlushPolicy
    {
        public IEventHandler EventHandler { get; set; }

        public void FlushEndOfBatch()
        {
        }

        public void FlushOnEvent()
        {
            EventHandler.Flush();
        }

        public void FlushWhenAhead()
        {
        }
    }    
}