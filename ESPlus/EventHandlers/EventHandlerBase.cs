using System.Collections.Generic;

namespace ESPlus.EventHandlers
{
    public abstract class EventHandlerBase<TContext> : IEventHandler<TContext>
        where TContext : IEventHandlerContext
    {
        protected TContext Context { get; private set; }

        public EventHandlerBase(TContext context)
        {
            Context = context;
        }

        public void Initialize()
        {
        }

        public virtual void Flush()
        {
            Context.Flush();
        }

        public abstract void DispatchEvent(object @event);
        public abstract IEnumerable<object> TakeEmittedEvents();
        public abstract IEnumerable<object> TakeEmittedOnSubmitEvents();
    }
}
