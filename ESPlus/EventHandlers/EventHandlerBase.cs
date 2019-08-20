using System.Collections.Generic;
using System.Threading.Tasks;
using ESPlus.Subscribers;

namespace ESPlus.EventHandlers
{
    public abstract class EventHandlerBase<TContext> : IEventHandler
        where TContext : IEventHandlerContext
    {
        private readonly IFlushPolicy _flushPolicy;
        protected TContext Context { get; private set; }
        protected readonly object _mutex = new object();

        IEventHandler IFlushPolicy.EventHandler
        {
            get => throw new System.NotImplementedException();
            set => throw new System.NotImplementedException();
        }

        public EventHandlerBase(TContext context, IFlushPolicy flushPolicy)
        {
            Context = context;
            flushPolicy.EventHandler = this;
            _flushPolicy = flushPolicy;
        }

        public Position Checkpoint
        {
            get => Context.Checkpoint;
            set => Context.Checkpoint = value;
        }

        public virtual void Initialize()
        {
        }

        public virtual void Flush()
        {
            lock (_mutex)
            {
                Context.Flush();
            }
        }

        public abstract bool DispatchEvent(object @event);
        public abstract IEnumerable<object> TakeEmittedEvents();
        public abstract IEnumerable<object> TakeEmittedOnSubmitEvents();
        public abstract bool Dispatch(Event @event);
        public abstract Task<bool> DispatchEventAsync(object @event);

        public virtual void Ahead()
        {
        }

        public void FlushWhenAhead()
        {
            _flushPolicy.FlushWhenAhead();
        }

        public void FlushEndOfBatch()
        {
            _flushPolicy.FlushEndOfBatch();
        }

        public void FlushOnEvent()
        {
            _flushPolicy.FlushOnEvent();
        }
    }
}
