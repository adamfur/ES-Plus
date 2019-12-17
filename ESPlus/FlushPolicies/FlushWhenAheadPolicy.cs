using ESPlus.EventHandlers;
using ESPlus.Subscribers;
using System;
using System.Threading;

namespace ESPlus.FlushPolicies
{
    public class FlushWhenAheadPolicy : IFlushPolicy
    {
        public IEventHandler EventHandler { get; set; }
        private readonly TimeSpan _timeout;
        private const int _eventThreshold = 100;
        private int _events = 0;

        public void FlushWhenAhead()
        {
            Flush();
        }

        public void FlushEndOfBatch()
        {
        }

        public void FlushOnEvent()
        {
            if (++_events > _eventThreshold)
            {
                Flush();
            }
        }

        private void Flush()
        {
            _events = 0;
            EventHandler.Flush();
        }
    }
    
//    public class FlushWhenAheadPolicy : IFlushPolicy
//    {
//        public IEventHandler EventHandler { get; set; }
//        private readonly TimeSpan _timeout;
//        private readonly object _mutex = new object();
//        private bool _doFlush = false;
//        private const int _eventThreshold = 100;
//        private int _events = 0;
//
//        public FlushWhenAheadPolicy(TimeSpan timeout)
//        {
//            _timeout = timeout;
//        }
//
//        public void Start()
//        {
//            new Thread(Worker).Start();
//        }
//
//        private void Worker()
//        {
//            while (true)
//            {
//                lock (_mutex)
//                {
//                    while (!_doFlush)
//                    {
//                        Monitor.Wait(_mutex);
//                    }
//                }
//                Thread.Sleep(_timeout);
//                Flush();
//            }
//        }
//
//        public void FlushWhenAhead()
//        {
//            lock (_mutex)
//            {
//                _doFlush = true;
//                Monitor.Pulse(_mutex);
//            }
//        }
//
//        public void FlushEndOfBatch()
//        {
//        }
//
//        public void FlushOnEvent()
//        {
//            if (++_events > _eventThreshold)
//            {
//                lock (_mutex)
//                {
//                    _doFlush = true;
//                    Monitor.Pulse(_mutex);
//                }
//            }
//        }
//
//        private void Flush()
//        {
//            lock (_mutex)
//            {
//                _doFlush = false;
//            }
//
//            _events = 0;
//            EventHandler.Flush();
//        }
//    }
}
