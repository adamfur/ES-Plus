using System.Collections.Generic;
using System.Linq;
using ESPlus.Aggregates;

namespace ESPlus.EventHandlers
{
    public class BasicEventHandler<TContext> : EventHandlerBase<TContext>
        where TContext : IEventHandlerContext
    {
        private readonly ConventionEventRouter _router = new ConventionEventRouter();
        private LinkedList<object> _emitQueue = new LinkedList<object>();
        private Dictionary<string, object> _emitOnSubmit = new Dictionary<string, object>();

        public BasicEventHandler(TContext context)
            : base(context)
        {
            _router.Register(this);
        }

        public override void DispatchEvent(object @event)
        {
            _router.Dispatch(@event);
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
    }
}
