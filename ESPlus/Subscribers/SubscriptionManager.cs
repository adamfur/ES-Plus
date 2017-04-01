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

        public SubscriptionManager(IEventFetcher eventFetcher, int workerThreads = 1)
        {
            _barrier = new Barrier(workerThreads);
            _eventFetcher = eventFetcher;
            foreach (var i in Enumerable.Range(0, workerThreads))
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
                NextPosition = position,
                RequestStatus = RequestStatus.Waiting,
                StarvedCycles = 0
            };

            _contexts.Add(context);
            return new SubscriptionClient(context);
        }

        public void Trigger()
        {
            Monitor.PulseAll(_mutex);
        }

        private static void WorkerThread(Barrier barrier, List<SubscriptionContext> contexts, object mutex, IEventFetcher eventFetcher)
        {
            barrier.SignalAndWait();

            while (true)
            {
                SubscriptionContext ctx;

                lock (mutex)
                {
                    while (contexts.All(x => x.RequestStatus == RequestStatus.Waiting) || !contexts.Any())
                    {
                        Monitor.Wait(mutex);
                    }

                    var waiting = contexts.Where(x => x.RequestStatus == RequestStatus.Waiting).ToList();

                    waiting.Sort();
                    waiting.ForEach(x => ++x.StarvedCycles);
                    ctx = waiting.First();
                    ctx.RequestStatus = RequestStatus.Fetching;
                }

                var events = eventFetcher.GetFromPosition(ctx.NextPosition);
                ctx.Put(events);
                ctx.RequestStatus = RequestStatus.Received;
            }
        }
    }
}