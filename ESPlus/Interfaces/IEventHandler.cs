using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ESPlus.MoonGoose;
using ESPlus.Subscribers;

namespace ESPlus.EventHandlers
{
    public interface IEventHandler : IFlushPolicy
    {
        Task<bool> DispatchEventAsync(object @event, CancellationToken cancellationToken);
        Task<bool> DispatchAsync(Event @event, CancellationToken cancellationToken);
        void Initialize();
        Task FlushAsync(CancellationToken cancellationToken);
        Position Checkpoint { get; set; }
        IEnumerable<object> TakeEmittedEvents();
        IEnumerable<object> TakeEmittedOnSubmitEvents();
        Task<object> Search(long[] parameters, string tenant, CancellationToken cancellationToken);
        Task<object> Get(string tenant, string path, CancellationToken cancellationToken);
        Task StartupAsync();
        Task Poke(int pokeType, string tenant, CancellationToken cancellationToken);
        Task<List<object>> List(string tenant, int size, int no, Box<int> total, CancellationToken cancellationToken);
        Task EvictCache();
    }
}
