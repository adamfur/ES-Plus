using System;
using System.Collections;
using System.Collections.Generic;
using ESPlus.EventHandlers;

namespace ESPlus.Subscribers
{
    public class SubscriptionManager : ISubscriptionManager
    {
        private readonly List<SubscriptionContext> _contexts = new List<SubscriptionContext>();

        public SubscriptionManager()
        {
        }

        public ISubscriptionClient Subscribe(long position, Priority priority = Priority.Normal)
        {
            var context = new SubscriptionContext
            {
                Priority = priority,
                Position = position,
                RequestStatus = RequestStatus.Waiting,
                StarvedCycles = 0
            };

            _contexts.Add(context);
            return new SubscriptionClient(context);
        }
    }

    public class SubscriptionContext
    {
        public Priority Priority { get; set; }
        public RequestStatus RequestStatus { get; set; }
        public long StarvedCycles { get; set; }
        public long Position { get; set; }
    }

    public class SubscriptionClient : ISubscriptionClient
    {
        private SubscriptionContext _subscriptionContext;

        public SubscriptionClient(SubscriptionContext subscriptionContext)
        {
            _subscriptionContext = subscriptionContext;
        }

        public IEnumerator<object> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}