using System.Collections.Generic;
using System.Threading.Tasks;
using ESPlus.Subscribers;

namespace ESPlus.EventHandlers
{
    public interface IEventHandler : IFlushPolicy
    {
        Task<bool> DispatchEventAsync(object @event);
        Task<bool> DispatchAsync(Event @event);
        void Initialize();
        Task FlushAsync();
        Position Checkpoint { get; set; }
        IEnumerable<object> TakeEmittedEvents();
        IEnumerable<object> TakeEmittedOnSubmitEvents();
        Task<object> Search(long[] parameters);
        Task<object> Get(string path);
    }
}
