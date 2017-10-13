using System.Collections.Generic;
using System.Threading.Tasks;
using ESPlus.Aggregates;
using ESPlus.Interfaces;
using System.Linq;
using System;
using ESPlus.EventHandlers;

namespace ESPlus
{
    public class InMemoryRepository : IRepository
    {
        private class EventStream
        {
            public EventNode First { get; set; }
            public EventNode Last { get; set; }
            public int Version { get; set; }
        }

        private class EventNode
        {
            public object Event { get; set; }
            public EventNode Next { get; set; }
            public EventNode NextInStream { get; set; }

            public EventNode(object @event)
            {
                Event = @event;
            }
        }

        private readonly Dictionary<string, EventStream> _streams = new Dictionary<string, EventStream>();
        private readonly List<IEventHandler> _subscribers = new List<IEventHandler>();
        private EventStream _all = new EventStream();

        public InMemoryRepository()
        {
            _all.First = _all.Last = new EventNode(null);
        }

        private void Subscribe(IEventHandler eventHandler)
        {
            _subscribers.Add(eventHandler);
        }

        public IEnumerable<object> AllEvents()
        {
            var enumerator = _all.First.Next;

            while (enumerator != null)
            {
                yield return enumerator.Event;

                enumerator = enumerator.Next;
            }
        }

        public Task AppendAsync<TAggregate>(TAggregate aggregate) where TAggregate : AppendableObject
        {
            return SaveImpl<TAggregate>(aggregate);
        }

        public Task DeleteStream<TAggregate>(string id) where TAggregate : IAggregate
        {
            var name = $"{typeof(TAggregate).Name}-{id}";

            _streams.Remove(name);
            return Task.WhenAll();
        }

        public Task<TAggregate> GetByIdAsync<TAggregate>(string id, int version = int.MaxValue) where TAggregate : ReplayableObject
        {
            var instance = (TAggregate)Activator.CreateInstance(typeof(TAggregate), id);
            var aggregate = (IAggregate)instance;
            var name = $"{typeof(TAggregate).Name}-{id}";

            if (_streams.ContainsKey(name))
            {
                var stream = _streams[name];
                var enumrator = stream.First;

                while (enumrator != null)
                {
                    aggregate.ApplyChange(enumrator.Event);
                    enumrator = enumrator.NextInStream;
                }
            }

            return Task.FromResult(instance);
        }

        public Task SaveAsync<TAggregate>(TAggregate aggregate) where TAggregate : ReplayableObject
        {
            return SaveImpl<TAggregate>(aggregate);
        }

        public Task SaveImpl<TAggregate>(TAggregate aggregate) where TAggregate : IAggregate
        {
            var uncommitedEvents = aggregate.TakeUncommittedEvents();
            var name = $"{typeof(TAggregate).Name}-{aggregate.Id}"; ;
            EventStream stream;

            if (_streams.ContainsKey(name))
            {
                stream = _streams[name];
            }
            else
            {
                _streams[name] = stream = new EventStream();
            }

            foreach (var @event in uncommitedEvents)
            {
                var node = new EventNode(@event);

                ++stream.Version;
                stream.First = stream.First ?? node;
                stream.Last.NextInStream = node;
                stream.Last = node;

                _all.Last.Next = node;
                _all.Last = node;

                NotifySubscribers(@event);
            }

            return Task.WhenAll();
        }

        private void NotifySubscribers(object @event)
        {
            foreach (var subscriber in _subscribers)
            {
                subscriber.DispatchEvent(@event);
            }
        }
    }
}