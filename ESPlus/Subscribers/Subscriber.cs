using ESPlus.EventHandlers;

namespace ESPlus.Subscribers
{
    public interface ISubscriber
    {
    }

    public class Subscriber : ISubscriber
    {
        private readonly IEventHandler<IEventHandlerContext> eventHandler;
        private readonly IFlushPolicy flushPolicy;

        public Subscriber(IEventHandler<IEventHandlerContext> eventHandler, IFlushPolicy flushPolicy)
        {
            this.flushPolicy = flushPolicy;
            this.eventHandler = eventHandler;
        }
    }

    public interface IFlushPolicy
    {
        bool OnEvent();
        bool OnBatch();
        bool OnIncompleteBatch();
    }
}