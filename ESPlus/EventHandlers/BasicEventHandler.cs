using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ESPlus.Aggregates;
using ESPlus.Misc;
using ESPlus.Storage;
using ESPlus.Subscribers;

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
            lock (Mutex)
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
            if (@event.Position == Position.Start)
            {
                Initialize();
            }


            Context.Checkpoint = @event.Position;
            //if (_router.CanHandle(@event.EventType))
            {
                //var type = _eventTypeResolver.ResolveType(@event.EventType);
                //var payload = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(@event.Payload), type);

                DispatchEvent(@event.DeserializedItem());
                return true;
            }
            return false;
        }

        public override Task<bool> DispatchEventAsync(object @event)
        {
            var result = this.DispatchEvent(@event);

            return Task.FromResult(result);
        }
    }
}
