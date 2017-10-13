using System;
using System.Collections.Generic;
using System.Threading;
using EventStore.ClientAPI;

namespace ESPlus.Subscribers
{
    public class SubscriptionContext : IComparable<SubscriptionContext>
    {
        private Queue<EventStream> _queue = new Queue<EventStream>();
        public Priority Priority { get; set; }
        public RequestStatus RequestStatus { get; set; }
        public long StarvedCycles { get; set; }
        public Position Position { get; set; }
        public SubscriptionManager Manager { get; set; }
        public Position Future { get; set; }
        public int QueueDepth { get; set; }
        public Action<Action> SynchronizedAction { get; set; }

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

                return (long)Priority + StarvedCycles;
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

            // if (StarvedCycles == other.StarvedCycles)
            // {
            //     return other.Priority.CompareTo(Priority);
            // }

            // if (Score == other.Score)
            // {
            //     return other.Priority.CompareTo(Priority);
            // }

            return other.Score.CompareTo(Score);
        }

        public EventStream Take()
        {
            lock (_queue)
            {
                while (_queue.Count == 0)
                {
                    // SynchronizedAction(() =>
                    // {
                    //     RequestStatus = RequestStatus.Waiting;
                    // });
                    Manager.TriggerContext(this);
                    Monitor.Wait(_queue);
                }

                var events = _queue.Dequeue();

                SynchronizedAction(() =>
                {
                    --QueueDepth;
                });
                return events;
            }
        }

        public void Put(EventStream events)
        {
            lock (_queue)
            {
                _queue.Enqueue(events);
                StarvedCycles = 0;
                Future = events.NextPosition;
                SynchronizedAction(() =>
                {
                    ++QueueDepth;
                });
                Monitor.Pulse(_queue);
            }
        }
    }
}