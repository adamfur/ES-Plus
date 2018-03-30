using ESPlus.EventHandlers;
using ESPlus.Subscribers;

namespace ESPlus
{
    public class NullFlushPolicy : IFlushPolicy
    {
        public IEventHandler EventHandler { get; set; }

        public void FlushEndOfBatch()
        {
        }

        public void FlushEvent()
        {
        }

        public void FlushWhenAhead()
        {
        }
    }
}