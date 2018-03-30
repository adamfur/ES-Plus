using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using EventStore.ClientAPI;

namespace ESPlus.Subscribers
{
    public class SubscriptionManager : ISubscriptionManager
    {
        private readonly List<SubscriptionContext> _contexts = new List<SubscriptionContext>();
        private Barrier _barrier;
        private object _mutex = new object();
        private IEventFetcher _eventFetcher;
        private int _workerThreads;

        public SubscriptionManager(IEventFetcher eventFetcher, int workerThreads = 1)
        {
            _workerThreads = workerThreads;
            _barrier = new Barrier(workerThreads);
            _eventFetcher = eventFetcher;
            eventFetcher.OnEventReceived(() => TriggerNewEvent());
        }

        public void TriggerNewEvent()
        {
            //Console.WriteLine("TriggerNewEvent");
            lock (_mutex)
            {
                _contexts.Where(x => x.RequestStatus == RequestStatus.Ahead).ToList()
                    .ForEach(x => x.RequestStatus = RequestStatus.Waiting);
                Monitor.Pulse(_mutex);
            }
        }

        public void Start()
        {
            for (var i = 0; i < _workerThreads; ++i)
            {
                var thread = new Thread(() => WorkerThread());
                thread.Start();
            }
        }

        private void Execute(Action action)
        {
            lock (_mutex)
            {
                action();
                Monitor.Pulse(_mutex);
            }
        }

        public ISubscriptionClient Subscribe(Position position, CancellationToken cancellationToken, Priority priority = Priority.Normal)
        {
            var context = new SubscriptionContext
            {
                Priority = priority,
                Position = position,
                RequestStatus = RequestStatus.Initialized,
                StarvedCycles = 0,
                Manager = this,
                Future = position,
                SynchronizedAction = Execute,
                CancellationToken = cancellationToken
            };

            lock (_mutex)
            {
                _contexts.Add(context);
                Monitor.Pulse(_mutex);
            }
            return new SubscriptionClient(context);
        }

        public void TriggerContext(SubscriptionContext subscriptionContext)
        {
            lock (_mutex)
            {
                subscriptionContext.RequestStatus = RequestStatus.Waiting;
                Monitor.Pulse(_mutex);
            }
        }

        public void TriggerSubscription()
        {
            lock (_mutex)
            {
                Monitor.Pulse(_mutex);
            }
        }

        private void WorkerThread()
        {
            _barrier.SignalAndWait();

            while (true)
            {
                SubscriptionContext subscriptionContext;

                lock (_mutex)
                {
                    while (!_contexts.Any(x => x.RequestStatus == RequestStatus.Waiting))
                    {
                        Monitor.Wait(_mutex);
                    }

                    var waiting = _contexts.Where(x => x.RequestStatus == RequestStatus.Waiting).ToList();

                    waiting.Sort();
                    waiting.ForEach(x => ++x.StarvedCycles);
                    subscriptionContext = waiting.First();
                    subscriptionContext.RequestStatus = RequestStatus.Fetching;
                }

                var events = _eventFetcher.GetFromPosition(subscriptionContext.Position);

                lock (_mutex)
                {
                    /*
                    Console.WriteLine($"{DateTime.Now:yyyy-MM-dd hh:mm:ss}: WorkerThread(long position = {subscriptionContext.Position.CommitPosition}), next: {events.NextPosition.CommitPosition}");
                    */
                    if (events.Events.Any())
                    {
                        subscriptionContext.RequestStatus = RequestStatus.Busy;
                        subscriptionContext.Put(events);
                    }
                    else
                    {
                        subscriptionContext.Put(EventStream.Ahead);
                        subscriptionContext.RequestStatus = RequestStatus.Ahead;
                        Console.WriteLine("!Ahead");
                    }
                }
            }
        }
    }
}