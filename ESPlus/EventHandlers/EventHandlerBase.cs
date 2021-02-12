using System.Collections.Generic;
using System.Threading.Tasks;
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

        public virtual async Task FlushAsync()
        {
            await Context.FlushAsync();
        }

        public abstract Task<bool> DispatchEventAsync(object @event);
        public abstract IEnumerable<object> TakeEmittedEvents();
        public abstract IEnumerable<object> TakeEmittedOnSubmitEvents();
        public abstract Task<object> Search(long[] parameters, string tenant);
        public abstract Task<object> Get(string path, string tenant);
        public abstract Task<bool> DispatchAsync(Event @event);

        public virtual void Ahead()
        {
        }

        public async Task FlushWhenAheadAsync()
        {
            await _flushPolicy.FlushWhenAheadAsync();
        }

        public async Task FlushOnEventAsync()
        {
            await _flushPolicy.FlushOnEventAsync();
        }
    }
}
