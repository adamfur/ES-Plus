using System;
using System.Collections.Generic;
using System.Linq;
using ESPlus.Interfaces;
using ESPlus.Misc;

namespace ESPlus.Aggregates
{
    public abstract class AggregateBase : IAggregate
    {
        private readonly Type _initialType;
        private readonly LinkedList<object> _uncommitedEvents = new LinkedList<object>();
        private readonly ConventionEventRouter _router = new ConventionEventRouter();
        public long Version { get; set; } = -1;
        public string Id { get; private set; }

        Type IAggregate.InitialType()
        {
            return _initialType;
        }

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

    public abstract class AggregateBase<T> : IAggregate<T> where T : IIdObject
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

        IEnumerable<object> IAggregate<T>.TakeUncommittedEvents()
        {
            if (_uncommittedEvents.TryDequeue(out var @event))
            {
                yield return @event;
            }

            yield break;
        }
    }
}
