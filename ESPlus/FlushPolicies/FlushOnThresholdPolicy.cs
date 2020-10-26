using ESPlus.EventHandlers;
using ESPlus.Subscribers;

namespace ESPlus.FlushPolicies
{
    public class FlushOnThresholdPolicy : IFlushPolicy
    {
        private const int EventThreshold = 100;
        private int _events = 0;

        public IEventHandler EventHandler { get; set; }

        public virtual void FlushWhenAhead()
        {
        }

        public void FlushOnEvent()
        {
            if (++_events > EventThreshold)
            {
                Flush();
            }
        }

        protected void Flush()
        {
            EventHandler.Flush();
            _events = 0;
        }
    }
}