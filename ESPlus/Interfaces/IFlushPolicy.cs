using ESPlus.EventHandlers;

namespace ESPlus.Subscribers
{
    // Event, Batch, IncompleteBatch, Time
    public interface IFlushPolicy
    {
        void FlushWhenAhead();
        void FlushOnEvent();
        IEventHandler EventHandler { get; set; }
    }
}