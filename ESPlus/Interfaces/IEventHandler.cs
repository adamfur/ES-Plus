using System.Collections.Generic;

namespace ESPlus.EventHandlers
{
    public interface IEventHandler<TContext>
        where TContext : IEventHandlerContext
    {
        void DispatchEvent(object @event);
        void Initialize();
        void Flush();
        IEnumerable<object> TakeEmittedEvents();
        IEnumerable<object> TakeEmittedOnSubmitEvents();
    }
}
