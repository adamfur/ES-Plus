using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ESPlus.MoonGoose;
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

        public virtual async Task FlushAsync(CancellationToken cancellationToken)
        {
            await Context.FlushAsync(cancellationToken);
        }

        public abstract Task<bool> DispatchEventAsync(object @event, CancellationToken cancellationToken);
        public abstract IEnumerable<object> TakeEmittedEvents();
        public abstract IEnumerable<object> TakeEmittedOnSubmitEvents();
        public abstract Task<object> Search(long[] parameters, string tenant, CancellationToken cancellationToken);
        public abstract Task<object> Get(string path, string tenant, CancellationToken cancellationToken);
        
        public virtual Task StartupAsync()
        {
            return Task.CompletedTask;
        }

        public virtual Task Poke(int pokeType, string tenant, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public abstract Task<List<object>> List(string tenant, int size, int no, Box<int> total, CancellationToken cancellationToken);
        public abstract IQueryable Query(string tenant, CancellationToken cancellationToken);

        public abstract Task<bool> DispatchAsync(Event @event, CancellationToken cancellationToken);

        public async Task EvictCache()
        {
            await Context.EvictCache();
        }

        public virtual async Task FlushWhenAheadAsync(CancellationToken cancellationToken)
        {
            await _flushPolicy.FlushWhenAheadAsync(cancellationToken);
        }

        public async Task FlushOnEventAsync(CancellationToken cancellationToken)
        {
            await _flushPolicy.FlushOnEventAsync(cancellationToken);
        }
    }
}
