using System.Collections.Generic;
using System.Threading.Tasks;
using ESPlus.Interfaces;
using System.Linq;
using System;
using System.Reflection;
using System.Threading;
using ESPlus.EventHandlers;
using ESPlus.Exceptions;
using ESPlus.Wyrm;

namespace ESPlus.Repositories
{
    public class InMemoryRepository : IWyrmRepository
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
        
        static InMemoryRepository()
        {
            var aggregates = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => x.GetTypes())
                .Where(x => typeof(IAggregate).IsAssignableFrom(x))
                .ToList();
        }
        
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
        
        public Task<TAggregate> GetByIdAsync<TAggregate>(string id, CancellationToken cancellationToken = default, long version = Int64.MaxValue)
            where TAggregate : IAggregate<string>
        {
            var instance = (TAggregate)Activator.CreateInstance(typeof(TAggregate), id);
            var aggregate = (IAggregate)instance;
            var currentVersion = 0;
        
            if (_streams.ContainsKey(id))
            {
                var stream = _streams[id];
                
                for (var enumerator = stream.First; enumerator != null && ++currentVersion <= version; enumerator = enumerator.NextInStream)
                {
                    var found = instance.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                        .Where(x => x.Name == "Apply" && x.ReturnType == typeof(void))
                        .Where(x => x.GetCustomAttribute(typeof(NoReplayAttribute)) == null)
                        .Any(x => x.GetParameters().First().ParameterType == enumerator.Event.GetType());
                    
                    
                    if (!found)
                    {
                        aggregate.Version = enumerator.Version;
                        continue;
                    }
                        
                    aggregate.ApplyChange(enumerator.Event);
                    aggregate.Version = enumerator.Version;
                }
            }
            else
            {
                throw new AggregateNotFoundException(id, null);
            }
        
            aggregate.TakeUncommittedEvents();
        
            return Task.FromResult(instance);
        }
        
        public async Task<WyrmResult> SaveAsync<TAggregate>(TAggregate aggregate, object headers, CancellationToken cancellationToken = default)
            where TAggregate : IAggregate<string>
        {
            await SaveImpl(aggregate, aggregate.Version);
            return new WyrmResult(Position.Start, 0);
        }
        
        public async Task<Position> SaveImpl(IAggregate<string> aggregate, long policy)
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
            
            return Position.Start;
        }
        
        private Task NotifySubscribers(object @event)
        {
            foreach (var subscriber in _subscribers)
            {
                // await subscriber.DispatchEventAsync(@event);
            }
        
            return Task.CompletedTask;
        }
        
        public Task DeleteAsync(string id, long version = -1, CancellationToken cancellationToken = default)
        {
            _streams.Remove(id);
            return Task.CompletedTask;
        }
    }
}