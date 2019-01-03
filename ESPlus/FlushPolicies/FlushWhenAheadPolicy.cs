using ESPlus.EventHandlers;
using ESPlus.Subscribers;
using System;
using System.Threading;

namespace ESPlus.FlushPolicies
{
    public class FlushWhenAheadPolicy : IFlushPolicy
    {
        public IEventHandler EventHandler { get; set; }
        private readonly int _ms;
        private object _mutex = new object();
        private bool _doFlush = false;
        private const int _eventThreshold = 100;
        private int _events = 0;

        public FlushWhenAheadPolicy(int ms)
        {
            _ms = ms;
        }

        public void Start()
        {
            new Thread(() => Worker()).Start();
        }

        private void Worker()
        {
            while (true)
            {
                lock (_mutex)
                {
                    while (!_doFlush)
                    {
                        Monitor.Wait(_mutex);
                    }
                }
                Thread.Sleep(_ms);
                Flush();
            }
        }

        public void FlushWhenAhead()
        {
            lock (_mutex)
            {
                _doFlush = true;
                Monitor.Pulse(_mutex);
            }
        }

        public void FlushEndOfBatch()
        {
        }

        public void FlushOnEvent()
        {
            if (++_events > _eventThreshold)
            {
                lock (_mutex)
                {
                    _doFlush = true;
                    Monitor.Pulse(_mutex);
                }
            }
        }

        private void Flush()
        {
            lock (_mutex)
            {
                _doFlush = false;
            }

            _events = 0;
            EventHandler.Flush();
        }
    }
}
