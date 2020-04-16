using ESPlus.EventHandlers;
using ESPlus.Subscribers;

namespace ESPlus.FlushPolicies
{
    public class FlushWhenAheadPolicy : IFlushPolicy
    {
        private const int EventThreshold = 100;
        private int _events = 0;

        public IEventHandler EventHandler { get; set; }

        public void FlushWhenAhead()
        {
            Flush();
        }

        public void FlushOnEvent()
        {
            if (++_events > EventThreshold)
            {
                Flush();
            }
        }

        private void Flush()
        {
            EventHandler.Flush();
            _events = 0;
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
//        }146:19
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
