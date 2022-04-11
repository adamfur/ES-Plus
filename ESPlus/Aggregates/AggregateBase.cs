using System;
using System.Collections.Generic;
using System.Linq;
using ESPlus.Interfaces;
using ESPlus.Misc;

namespace ESPlus.Aggregates
{
    public abstract class AggregateBase<T> : IAggregate<T>
    {
        private readonly Type _initialType;
        private readonly Queue<object> _uncommittedEvents = new Queue<object>();
        private readonly ConventionEventRouter _router = new ConventionEventRouter();
        public long Version { get; set; } = -1;
        public T Id { get; }

        Type IAggregate<T>.InitialType()
        {
            return _initialType;
        }

        protected AggregateBase(T id, Type initialType = null)
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

        void IAggregate<T>.ApplyChange(object @event)
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
            _uncommittedEvents.Enqueue(@event);
            ++Version;
        }

        protected void ApplyChange(object @event)
        {
            ((IAggregate<T>)this).ApplyChange(@event);
        }

        List<object> IAggregate<T>.TakeUncommittedEvents()
        {
            var result = _uncommittedEvents.ToList();
            
            _uncommittedEvents.Clear();
            return result;
        }
    }

    public abstract class AggregateBase : AggregateBase<string>, IAggregate
    {
        protected AggregateBase(string id, Type initialType = null) : base(id, initialType)
        {
        }
    }
}
