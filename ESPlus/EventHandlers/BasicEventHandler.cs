using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ESPlus.Misc;
using ESPlus.MoonGoose;
using ESPlus.Storage;
using ESPlus.Subscribers;
using Wyrm;

namespace ESPlus.EventHandlers
{
    public class BasicEventHandler<TContext> : EventHandlerBase<TContext>
        where TContext : class, IEventHandlerContext 
    {
        private readonly ConventionEventRouterAsync _router = new ConventionEventRouterAsync();
        private readonly LinkedList<object> _emitQueue = new LinkedList<object>();
        private readonly Dictionary<string, object> _emitOnSubmit = new Dictionary<string, object>();
        private readonly IEventTypeResolver _eventTypeResolver;
        private readonly IEventSerializer _eventSerializer;
        private readonly Once _once;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        public BasicEventHandler(TContext context, IEventTypeResolver eventTypeResolver, IFlushPolicy flushPolicy, IEventSerializer eventSerializer)
            : base(context, flushPolicy)
        {
            _once = new Once(() =>
            {
                RegisterRouter(_router);
            });
            _eventTypeResolver = eventTypeResolver;
            _eventSerializer = eventSerializer;
        }

        protected virtual void RegisterRouter(ConventionEventRouterAsync router)
        {
            router.Register(this);
        }

        public override async Task<bool> DispatchEventAsync(object @event, CancellationToken cancellationToken)
        {
            await using var guard = await SemaphoreGuard.Build(_semaphore);
            _once.Execute();
            await _router.DispatchAsync(@event, cancellationToken);
            return true;
        }

        protected void Emit(object @event)
        {
            _emitQueue.AddLast(@event);
        }

        protected void EmitOnSubmit(string key, object @event)
        {
            _emitOnSubmit[key] = @event;
        }

        public override IEnumerable<object> TakeEmittedEvents()
        {
            var result = _emitQueue.ToList();

            _emitQueue.Clear();
            return result;
        }

        public override IEnumerable<object> TakeEmittedOnSubmitEvents()
        {
            var result = _emitOnSubmit.Values.ToList();

            _emitOnSubmit.Clear();
            return result;
        }

        public override async Task<object> Search(long[] parameters, string tenant, CancellationToken cancellationToken)
        {
            await using var guard = await SemaphoreGuard.Build(_semaphore);
            return DoSearch(parameters, tenant, cancellationToken);
        }

        public override async Task<object> Get(string tenant, string path, CancellationToken cancellationToken)
        {
            await using var guard = await SemaphoreGuard.Build(_semaphore);
            return DoGet(tenant, path, cancellationToken);
        }

        public override async Task<List<object>> List(string tenant, int size, int no, Box<int> total, CancellationToken cancellationToken)
        {
            await using var guard = await SemaphoreGuard.Build(_semaphore);
            return await DoList(tenant, size, no, total, cancellationToken);
        }

        public override IQueryable Query(string tenant, CancellationToken cancellationToken)
        {
            return DoQuery(tenant, cancellationToken);
        }

        protected virtual Task<object> DoSearch(long[] parameters, string tenant, CancellationToken cancellationToken)
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

        public override async Task<bool> DispatchAsync(Event @event, CancellationToken cancellationToken)
        {
            if (@event.InitEvent)
            {
                Initialize();
                return false;
            }

            var status = false;

            Context.TimestampUtc = @event.TimestampUtc;
            Context.Checkpoint = @event.Position;
            Context.Offset = @event.Offset;
            Context.TotalOffset = @event.TotalOffset;
            Context.StreamName = @event.StreamName;
            Context.Metadata = new MetaData(@event.Meta, _eventSerializer);
            _once.Execute();
            
            if (@event.EventType == typeof(StreamCreated).FullName)
            {
                try
                {
                    var type = _eventTypeResolver.ResolveType(@event.CreateEvent);
                
                    if (type != null)
                    {
                        var instance = BuildCreatedStream(type, @event.StreamName);
                    
                        await DispatchEventAsync(instance, cancellationToken);
                    }
                }
                catch (ArgumentException)
                {
                }
            
                status = true;
            }
            else if (@event.EventType == typeof(StreamDeleted).FullName)
            {
                try
                {
                    var type = _eventTypeResolver.ResolveType(@event.DeleteEvent);
                
                    if (type != null)
                    {
                        var instance = BuildDeletedStream(type, @event.StreamName);
                    
                        await DispatchEventAsync(instance, cancellationToken);
                    }
                }
                catch (ArgumentException)
                {
                }
            
                status = true;
            }
            else if (_router.CanHandle(@event.EventType))
            {
                await DispatchEventAsync(@event.DeserializedItem(), cancellationToken);
                status = true;
            }
            
            if (@event.IsAhead)
            {
                await FlushWhenAheadAsync(cancellationToken);
            }

            return status;
        }
        
        private StreamCreated BuildCreatedStream(Type type, string createEventType)
        {
            var make = typeof(StreamCreated<>).MakeGenericType(type);
            var result = (StreamCreated) Activator.CreateInstance(make, createEventType);

            return result;
        }  
        
        private StreamDeleted BuildDeletedStream(Type type, string createEventType)
        {
            var make = typeof(StreamDeleted<>).MakeGenericType(type);
            var result = (StreamDeleted) Activator.CreateInstance(make, createEventType);

            return result;
        }        
    }
}
