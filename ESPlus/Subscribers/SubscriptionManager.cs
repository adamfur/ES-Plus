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
            foreach (var i in Enumerable.Range(0, _workerThreads))
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

            _contexts.Add(context);
            return new SubscriptionClient(context);
        }

        public void Trigger(SubscriptionContext subscriptionContext)
        {
            lock (_mutex)
            {
                subscriptionContext.RequestStatus = RequestStatus.Waiting;
                Monitor.PulseAll(_mutex);
            }
        }

        private static void WorkerThread(Barrier barrier, List<SubscriptionContext> contexts, object mutex, IEventFetcher eventFetcher)
        {
            Console.WriteLine("WorkerThread.Barrier");
            barrier.SignalAndWait();
            Console.WriteLine("WorkerThread.Barrier.Release");

            while (true)
            {
                SubscriptionContext ctx;

                lock (mutex)
                {
                    while (!contexts.Any(x => x.RequestStatus == RequestStatus.Waiting))
                    {
                        Monitor.Wait(mutex);
                    }

                    var waiting = contexts.Where(x => x.RequestStatus == RequestStatus.Waiting).ToList();

                    waiting.Sort();
                    waiting.ForEach(x => ++x.StarvedCycles);
                    ctx = waiting.First();
                    ctx.RequestStatus = RequestStatus.Fetching;
                }

                var events = eventFetcher.GetFromPosition(ctx.Position);

                lock (mutex)
                {
                    ctx.Put(events);
                    ctx.RequestStatus = RequestStatus.Busy;
                }
            }
        }
    }
}