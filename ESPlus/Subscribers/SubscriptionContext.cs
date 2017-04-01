using System;
using System.Collections.Generic;
using System.Threading;

namespace ESPlus.Subscribers
{
    public class SubscriptionContext : IComparable<SubscriptionContext>
    {
        private Queue<IEnumerable<Event>> _queue = new Queue<IEnumerable<Event>>();
        public Priority Priority { get; set; }
        public RequestStatus RequestStatus { get; set; }
        public long StarvedCycles { get; set; }
        public long Position { get; set; }
        public SubscriptionManager Manager { get; internal set; }

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
                    return (long)Priority;
                }

                return (long)Priority * StarvedCycles;
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

        public IEnumerable<Event> Take()
        {
            lock (_queue)
            {
                while (_queue.Count == 0)
                {
                    Manager.Trigger(this);
                    Monitor.Wait(_queue);
                }

                var events = _queue.Dequeue();

                return events;
            }
        }

        public void Put(IEnumerable<Event> events)
        {
            lock (_queue)
            {
                _queue.Enqueue(events);
                StarvedCycles = 0;
                Monitor.Pulse(_queue);
            }
        }
    }
}