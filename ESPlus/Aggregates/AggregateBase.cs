using System;
using System.Collections.Generic;
using System.Linq;
using ESPlus.Interfaces;

namespace ESPlus.Aggregates
{
    public abstract class AggregateBase : IAggregate
    {
        private readonly Type _initialType;
        private readonly LinkedList<object> _uncommitedEvents = new LinkedList<object>();
        private readonly ConventionEventRouter _router = new ConventionEventRouter();
        public long Version { get; set; } = -1;
        public string Id { get; private set; }

        protected AggregateBase(string id, Type initialType = null)
        {
            _initialType = initialType;
            Id = id;
            _router.Register(this);
        }

        protected virtual void Invoke(object @event)
        {
            try
            {
                _router.Dispatch(@event);
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($":: {@event.GetType().Name} {ex}");
                throw;
            }
        }

        void IAggregate.ApplyChange(object @event)
        {
            if (Version == -1)
            {
                if (_initialType != null)
                {
                    if (@event.GetType() != _initialType)
                    {
                        throw new Exception("Invalid Aggregate");
                    }
                }
            }

            Invoke(@event);
            _uncommitedEvents.AddLast(@event);
            ++Version;
        }

        protected void ApplyChange(object @event)
        {
            ((IAggregate)this).ApplyChange(@event);
        }

        public IEnumerable<object> TakeUncommittedEvents()
        {
            var result = _uncommitedEvents.ToList();

            _uncommitedEvents.Clear();
            return result;
        }
    }
}
