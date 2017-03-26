using System.Collections.Generic;
using System.Linq;
using ESPlus.Interfaces;

namespace ESPlus.Aggregates
{

    public abstract class AggregateBase : IAggregate
    {
        private readonly LinkedList<object> _uncommitedEvents = new LinkedList<object>();
        private readonly ConventionEventRouter _router = new ConventionEventRouter();

        protected AggregateBase(string id)
        {
            Id = id;
            _router.Register(this);
        }

        public int Version { get; private set; } = 0;
        public string Id { get; private set; }

        protected virtual void Invoke(object @event)
        {
            _router.Dispatch(@event);
        }

        void IAggregate.ApplyChange(object @event)
        {
            Invoke(@event);
            _uncommitedEvents.AddLast(@event);
            ++Version;
        }

        protected void ApplyChange(object @event)
        {
            ((IAggregate) this).ApplyChange(@event);
        }

        public IEnumerable<object> TakeUncommitedEvents()
        {
            var result = _uncommitedEvents.ToList();

            _uncommitedEvents.Clear();
            return result;
        }
    }
}
