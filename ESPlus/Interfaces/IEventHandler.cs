using System.Collections.Generic;
using System.Threading.Tasks;
using ESPlus.Subscribers;

namespace ESPlus.EventHandlers
{
    public interface IEventHandler : IFlushPolicy
    {
        bool DispatchEvent(object @event);
        // Task<bool> DispatchEventAsync(object @event);
        bool Dispatch(Event @event);
        void Initialize();
        void Flush();
        Position Checkpoint { get; set; }
        IEnumerable<object> TakeEmittedEvents();
        IEnumerable<object> TakeEmittedOnSubmitEvents();
    }
}
