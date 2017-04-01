using System.Collections.Generic;
using System.Linq;
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
}