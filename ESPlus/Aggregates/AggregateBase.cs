using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using ESPlus.Interfaces;
using ESPlus.Wyrm;

namespace ESPlus.Aggregates
{
    public abstract class AggregateBase : IAggregate
    {
        public Type InitialType { get; }
        private readonly LinkedList<object> _uncommittedEvents = new LinkedList<object>();
        private readonly ConventionEventRouter _router = new ConventionEventRouter();
        public long Version { get; set; } = -1;
        public string Id { get; }

        protected AggregateBase(string id, Type initialType = null)
        {
            InitialType = initialType;
            Id = id;
            _router.Register(this);
        }

        protected virtual void Invoke(object @event)
        {
            _router.Dispatch(@event);
        }

        void IAggregate.ApplyChange(object @event)
        {
            if (Version == -1)
            {
                if (InitialType != null)
                {
                    if (@event.GetType() != InitialType)
                    {
                        throw new Exception("Invalid Aggregate");
                    }
                }
            }

            Invoke(@event);
            _uncommittedEvents.AddLast(@event);
            ++Version;
        }

        protected void ApplyChange(object @event)
        {
            ((IAggregate) this).ApplyChange(@event);
        }

        public IEnumerable<object> TakeUncommittedEvents()
        {
            var result = _uncommittedEvents.ToList();

            _uncommittedEvents.Clear();
            return result;
        }

        public void Visit(WyrmEventItem item)
        {
            var obj = item.Serializer.Deserialize(item.EventType, item.Data);

            ApplyChange(obj);
            _uncommittedEvents.Clear();
        }

        public void Visit(WyrmAheadItem item)
        {
        }

        public void Visit(WyrmVersionItem item)
        {
            Version = item.StreamVersion;
        }

        public void Visit(WyrmDeleteItem item)
        {
        }

        public IEnumerable<Type> ApplyTypes()
        {
            return GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(x => x.Name == "Apply" && x.ReturnType == typeof(void))
                .Where(x => x.GetCustomAttribute(typeof(NoReplayAttribute)) == null)
                .Select(x => x.GetParameters().Single().ParameterType);
        }
    }
}
