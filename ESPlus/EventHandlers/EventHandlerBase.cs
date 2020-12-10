using System.Collections.Generic;
using System.Threading.Tasks;
using ESPlus.Subscribers;

namespace ESPlus.EventHandlers
{
    public abstract class EventHandlerBase<TContext> : IEventHandler
        where TContext : class, IEventHandlerContext
    {
        private readonly IFlushPolicy _flushPolicy;
        protected TContext Context { get; private set; }

        IEventHandler IFlushPolicy.EventHandler
        {
            get => throw new System.NotImplementedException();
            set => throw new System.NotImplementedException();
        }

        protected EventHandlerBase(TContext context, IFlushPolicy flushPolicy)
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
            Context.Flush();
        }

        public abstract bool DispatchEvent(object @event);
        public abstract IEnumerable<object> TakeEmittedEvents();
        public abstract IEnumerable<object> TakeEmittedOnSubmitEvents();
        public abstract Task<object> Search(long[] parameters);
        public abstract Task<object> Get(string path);
        public abstract bool Dispatch(Event @event);

        public virtual void Ahead()
        {
        }

        public void FlushWhenAhead()
        {
            _flushPolicy.FlushWhenAhead();
        }

        public void FlushOnEvent()
        {
            _flushPolicy.FlushOnEvent();
        }
    }
}
