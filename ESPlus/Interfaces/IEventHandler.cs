using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ESPlus.Subscribers;

namespace ESPlus.EventHandlers
{
    public interface IEventHandler : IFlushPolicy
    {
        Task<bool> DispatchEventAsync(object @event);
        Task<bool> DispatchAsync(Event @event, CancellationToken cancellationToken);
        void Initialize();
        Task FlushAsync();
        Position Checkpoint { get; set; }
        IEnumerable<object> TakeEmittedEvents();
        IEnumerable<object> TakeEmittedOnSubmitEvents();
        Task<object> Search(long[] parameters, string tenant);
        Task<object> Get(string path, string tenant);
        Task StartupAsync();
    }
}
