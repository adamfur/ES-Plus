using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ESPlus.Aggregates;
using ESPlus.Misc;
using ESPlus.Storage;
using ESPlus.Subscribers;
using Newtonsoft.Json;
using Wyrm;

namespace ESPlus.EventHandlers
{
    public class BasicEventHandler<TContext> : EventHandlerBase<TContext>
        where TContext : IEventHandlerContext
    {
        protected readonly ConventionEventRouter _router = new ConventionEventRouter();
        private LinkedList<object> _emitQueue = new LinkedList<object>();
        private Dictionary<string, object> _emitOnSubmit = new Dictionary<string, object>();
        private readonly IEventTypeResolver _eventTypeResolver;
        protected Once _once;

        public BasicEventHandler(TContext context, IEventTypeResolver eventTypeResolver, IFlushPolicy flushPolicy)
            : base(context, flushPolicy)
        {
            _once = new Once(() =>
            {
                RegisterRouter(_router);
            });
            _eventTypeResolver = eventTypeResolver;
        }

        protected virtual void RegisterRouter(ConventionEventRouter router)
        {
            router.Register(this);
        }

        public override bool DispatchEvent(object @event)
        {
            lock (_mutex)
            {
                _once.Execute();
                _router.Dispatch(@event);
                return true;
            }
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

        public override bool Dispatch(Event @event)
        {
            if (@event.Offset == 1)
            {
                Initialize();
            }

            var status = false;

            Context.Checkpoint = @event.Position;
            Context.Offset = @event.Offset;
            Context.TotalOffset = @event.TotalOffset;
            Context.Metadata = new MetaData(@event.Meta);
            _once.Execute();

//            {
//                Console.WriteLine($"{@event.StreamName}: {@event.EventType}, Offset: {@event.Offset}, Ahead: {@event.IsAhead}");
//
//                if (@event.EventType != typeof(StreamDeleted).FullName)
//                {
//                    Console.WriteLine(JsonConvert.SerializeObject(@event.DeserializedItem(), Formatting.Indented));
//                }
//            }
   
            if (@event.EventType == typeof(StreamDeleted).FullName)
            {
                try
                {
                    var type = _eventTypeResolver.ResolveType(@event.CreateEvent);
                
                    if (type != null)
                    {
                        var instance = CreateInstance(type, @event.StreamName);
                    
//                        Console.WriteLine($"<@@@> Dispatch: StreamDeleted!{@event.CreateEvent}");
                        DispatchEvent(instance);
                    }
                }
                catch (ArgumentException)
                {
                }

                status = true;
            }
            else if (_router.CanHandle(@event.EventType))
            {
                DispatchEvent(@event.DeserializedItem());
                status = true;
            }

            if (@event.IsAhead)
            {
                Ahead();
            }

            return status;
        }
        
        private StreamDeleted CreateInstance(Type type, string createEventType)
        {
            var make = typeof(StreamDeleted<>).MakeGenericType(new[] { type });
            var result = (StreamDeleted) Activator.CreateInstance(make, createEventType);

            return result;
        }        

        public override Task<bool> DispatchEventAsync(object @event)
        {
            var result = this.DispatchEvent(@event);

            return Task.FromResult(result);
        }
    }
}
