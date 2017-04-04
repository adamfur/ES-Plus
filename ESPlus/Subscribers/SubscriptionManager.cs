using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ESPlus.Subscribers
{
    public class SubscriptionManager : ISubscriptionManager
    {
        private readonly List<SubscriptionContext> _contexts = new List<SubscriptionContext>();
        private Barrier _barrier;
        private object _mutex = new object();
        private IEventFetcher _eventFetcher;
        private int _workerThreads;

        public SubscriptionManager(IEventFetcher eventFetcher, int workerThreads)
        {
            _workerThreads = workerThreads;
            _barrier = new Barrier(workerThreads);
            _eventFetcher = eventFetcher;
        }

        public void Start()
        {
            for (int i = 0; i < _workerThreads; ++i)
            {
                var thread = new Thread(() => WorkerThread(_barrier, _contexts, _mutex, _eventFetcher));
                thread.Start();
            }
        }

        public ISubscriptionClient Subscribe(long position, Priority priority = Priority.Normal)
        {
            var context = new SubscriptionContext
            {
                Priority = priority,
                Position = position,
                RequestStatus = RequestStatus.Waiting,
                StarvedCycles = 0,
                Manager = this
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
                Console.WriteLine("public void TriggerContext(SubscriptionContext subscriptionContext)");
                subscriptionContext.RequestStatus = RequestStatus.Waiting;
                Monitor.Pulse(_mutex);
            }
        }

        public void TriggerSubscription()
        {
            lock (_mutex)
            {
                // new event from subscription!!!
                Monitor.Pulse(_mutex);
            }
        }

        private static void WorkerThread(Barrier barrier, List<SubscriptionContext> contexts, object mutex, IEventFetcher eventFetcher)
        {
            barrier.SignalAndWait();

            while (true)
            {
                SubscriptionContext subscriptionContext;

                lock (mutex)
                {
                    while (!contexts.Any(x => x.RequestStatus == RequestStatus.Waiting))
                    {
                        Monitor.Wait(mutex);
                    }

                    var waiting = contexts.Where(x => x.RequestStatus == RequestStatus.Waiting).ToList();

                    waiting.Sort();
                    waiting.ForEach(x => ++x.StarvedCycles);
                    subscriptionContext = waiting.First();

                    Console.WriteLine($"{subscriptionContext.RequestStatus}");


                    subscriptionContext.RequestStatus = RequestStatus.Fetching;
                }

                var events = eventFetcher.GetFromPosition(subscriptionContext.Position);

                lock (mutex)
                {
                    if (events.Any())
                    {
                        Console.WriteLine($"Put {events.Count()}");
                        subscriptionContext.RequestStatus = RequestStatus.Busy;
                        subscriptionContext.Put(events);
                    }
                    else
                    {
                        Console.WriteLine($"!Put");
                        subscriptionContext.RequestStatus = RequestStatus.Ahead;
                    }
                }
            }
        }
    }
}