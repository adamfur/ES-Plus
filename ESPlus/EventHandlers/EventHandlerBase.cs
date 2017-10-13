using System.Collections.Generic;
using ESPlus.Subscribers;

namespace ESPlus.EventHandlers
{
    public abstract class EventHandlerBase<TContext> : IEventHandler
        where TContext : IEventHandlerContext
    {
        protected TContext Context { get; private set; }

        public EventHandlerBase(TContext context)
        {
            Context = context;
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
        public abstract bool Dispatch(Event @event);
    }
}
