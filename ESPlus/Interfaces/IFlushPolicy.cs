using ESPlus.EventHandlers;

namespace ESPlus.Subscribers
{
    // Event, Batch, IncompleteBatch, Time
    public interface IFlushPolicy
    {
        void FlushWhenAhead();
        void FlushEndOfBatch();
        void FlushEvent();
        IEventHandler EventHandler { get; set; }
    }
}