using System;
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

    public class SubscriptionContext : IComparable<SubscriptionContext>
    {
        public Queue<List<object>> Queue = new Queue<List<object>>();
        public Priority Priority { get; set; }
        public RequestStatus RequestStatus { get; set; }
        public long StarvedCycles { get; set; }
        public long Position { get; set; }

        public long Score
        {
            get
            {
                if (Priority == Priority.RealTime || Priority == Priority.Idle)
                {
                    return 0;
                }

                if (StarvedCycles == 0)
                {
                    return (long) Priority;
                }

                return (long) Priority * StarvedCycles;
            }
        }

        public int CompareTo(SubscriptionContext other)
        {
            if (Priority != other.Priority)
            {
                if (other.Priority == Priority.RealTime)
                {
                    return 1;
                }
                else if (Priority == Priority.RealTime)
                {
                    return -1;
                }

                if (other.Priority == Priority.Idle)
                {
                    return -1;
                }
                else if (Priority == Priority.Idle)
                {
                    return 1;
                }
            }

            if (StarvedCycles == other.StarvedCycles)
            {
                return other.Priority.CompareTo(Priority);
            }

            if (Score == other.Score)
            {
                return other.Priority.CompareTo(Priority);
            }

            return other.Score.CompareTo(Score);
        }

        public IEnumerable<object> Take()
        {
            RequestStatus = RequestStatus.Waiting;
            RequestStatus = RequestStatus.Busy;

            throw new NotImplementedException();
        }
    }
}