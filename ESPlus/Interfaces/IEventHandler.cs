using System.Collections.Generic;
using ESPlus.Subscribers;

namespace ESPlus.EventHandlers
{
    public interface IEventHandler
    {
        bool DispatchEvent(object @event);
        bool Dispatch(Event @event);
        void Initialize();
        void Flush();
        IEnumerable<object> TakeEmittedEvents();
        IEnumerable<object> TakeEmittedOnSubmitEvents();
    }
}
