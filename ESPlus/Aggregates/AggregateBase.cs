using System.Collections.Generic;
using System.Linq;
using ESPlus.Interfaces;

namespace ESPlus.Aggregates
{
    public abstract class AggregateBase : IAggregate
    {
        private readonly LinkedList<object> _uncommitedEvents = new LinkedList<object>();

        protected AggregateBase(string id)
        {
            Id = id;
        }

        public int Version { get; private set; } = 0;
        public string Id { get; private set; }

        protected virtual void Invoke(object @event)
        {
        }

        void IAggregate.ApplyChange(object @event)
        {
            Invoke(@event);
            _uncommitedEvents.AddLast(@event);
            ++Version;
        }

        public IEnumerable<object> TakeUncommitedEvents()
        {
            var result = _uncommitedEvents.ToList();

            _uncommitedEvents.Clear();
            return result;
        }
    }
}
