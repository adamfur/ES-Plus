using System;
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
        protected readonly IFlushPolicy FlushPolicy;
        protected TContext Context { get; private set; }
        protected readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1);
        
        public void ScheduleFlush()
        {
            FlushPolicy.ScheduleFlush();
        }

        IEventHandler IFlushPolicy.EventHandler
        {
            get => throw new System.NotImplementedException();
            set => throw new System.NotImplementedException();
        }

        protected EventHandlerBase(TContext context, IFlushPolicy flushPolicy)
        {
            Context = context;
            flushPolicy.EventHandler = this;
            FlushPolicy = flushPolicy;
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
        
        public virtual Task StartupAsync()
        {
            return Task.CompletedTask;
        }

        public virtual Task Poke(int pokeType, string tenant, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public abstract Task<bool> DispatchAsync(Event @event, CancellationToken cancellationToken);

        public async Task EvictCache()
        {
            await Context.EvictCache();
        }

        public virtual async Task FlushWhenAheadAsync(CancellationToken cancellationToken)
        {
            await FlushPolicy.FlushWhenAheadAsync(cancellationToken);
        }

        public async Task FlushOnEventAsync(CancellationToken cancellationToken)
        {
            await FlushPolicy.FlushOnEventAsync(cancellationToken);
        }
        
        public async Task<object> Search(string tenant, long[] parameters, CancellationToken cancellationToken)
        {
            await using var guard = await SemaphoreGuard.Build(Semaphore);
            return await DoSearch(tenant, parameters, cancellationToken);
        }

        public async Task<object> Get(string tenant, string path, CancellationToken cancellationToken)
        {
            await using var guard = await SemaphoreGuard.Build(Semaphore);
            return await DoGet(tenant, path, cancellationToken);
        }

        public async Task<List<object>> List(string tenant, int size, int no, Box<int> total, CancellationToken cancellationToken)
        {
            await using var guard = await SemaphoreGuard.Build(Semaphore);
            return await DoList(tenant, size, no, total, cancellationToken);
        }

        public IQueryable Query(string tenant, CancellationToken cancellationToken)
        {
            return DoQuery(tenant, cancellationToken);
        }
        
        protected virtual Task<object> DoSearch(string tenant, long[] parameters, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        protected virtual Task<object> DoGet(string tenant, string path, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        protected virtual Task<List<object>> DoList(string tenant, int size, int no, Box<int> total, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        protected virtual IQueryable DoQuery(string tenant, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
