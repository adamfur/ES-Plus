using System.Collections.Generic;
using System.Threading.Tasks;
using ESPlus.Aggregates;
using ESPlus.Interfaces;
using System.Linq;
using System;
using ESPlus.EventHandlers;

namespace ESPlus.Repositories
{
    public class InMemoryRepository : IRepository
    {
        private class EventStream
        {
            public EventNode First { get; set; }
            public EventNode Last { get; set; }
            public long Version { get; set; } = -1;
        }

        private class EventNode
        {
            public object Event { get; set; }
            public long Version { get; set; }
            public EventNode Next { get; set; }
            public EventNode NextInStream { get; set; }

            public EventNode(object @event, long version)
            {
                Event = @event;
                Version = version;
            }
        }

        private readonly Dictionary<string, EventStream> _streams = new Dictionary<string, EventStream>();
        private readonly List<IEventHandler> _subscribers = new List<IEventHandler>();
        private EventStream _all = new EventStream();

        public InMemoryRepository()
        {
            _all.First = _all.Last = new EventNode(null, -1);
        }

        public void RegisterSubscriber(IEventHandler eventHandler)
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

        public Task DeleteStream<TAggregate>(string id) where TAggregate : IAggregate
        {
            _streams.Remove(id);
            return Task.WhenAll();
        }

        public Task<TAggregate> GetByIdAsync<TAggregate>(string id, int version = int.MaxValue) where TAggregate : IAggregate
        {
            var instance = (TAggregate)Activator.CreateInstance(typeof(TAggregate), id);
            var aggregate = (IAggregate)instance;
            var currentVersion = 0;

            if (_streams.ContainsKey(id))
            {
                var stream = _streams[id];
                var enumrator = stream.First;

                while (enumrator != null && ++currentVersion <= version)
                {
                    //Console.WriteLine($"{enumrator.Event.GetType()} {++count}");
                    aggregate.ApplyChange(enumrator.Event);
                    aggregate.Version = enumrator.Version;
                    enumrator = enumrator.NextInStream;
                }
            }

            aggregate.TakeUncommittedEvents();

            return Task.FromResult(instance);
        }

        public Task SaveAsync(AggregateBase aggregate)
        {
            return SaveImpl(aggregate, aggregate.Version);
        }

        public Task AppendAsync(AggregateBase aggregate)
        {
            return SaveImpl(aggregate, WritePolicy.Any);
        }

        public Task SaveNewAsync(IAggregate aggregate)
        {
            return SaveImpl(aggregate, WritePolicy.EmptyStream);
        }

        public async Task SaveImpl(IAggregate aggregate, long policy)
        {
            var events = aggregate.TakeUncommittedEvents().ToList();
            EventStream stream;

            if (_streams.ContainsKey(aggregate.Id))
            {
                stream = _streams[aggregate.Id];
            }
            else
            {
                _streams[aggregate.Id] = stream = new EventStream();
            }

            if (policy >= 0 && stream.Version != aggregate.Version - events.Count())
            {
                throw new WrongExpectedVersionException();
            }
            else if (policy == WritePolicy.NoStream && stream.Version != -1)
            {
                throw new WrongExpectedVersionException();
            }

            foreach (var @event in events)
            {
                var version = ++stream.Version;
                var node = new EventNode(@event, version);

                if (stream.First == null)
                {
                    stream.First = node;
                    stream.Last = node;
                }
                else
                {
                    stream.Last.NextInStream = node;
                    stream.Last = node;
                }

                _all.Last.Next = node;
                _all.Last = node;
            };

            foreach (var @event in events)
            {
                //Console.WriteLine(@event);
                await NotifySubscribers(@event);
            };
        }

        private async Task NotifySubscribers(object @event)
        {
            foreach (var subscriber in _subscribers)
            {
                await subscriber.DispatchEventAsync(@event);
            }
        }

        public Task DeleteAsync(string streamName)
        {
            return Task.FromResult(0);
        }
    }
}