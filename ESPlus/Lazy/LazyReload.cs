using System;
using System.Threading;

namespace ESPlus.Lazy
{
    public class LazyReload<T>
    {
        private T _instance = default(T);
        private readonly Func<T> _func;
        private readonly Thread _thread;
        private readonly Mutex _mutex = new Mutex();
        private bool _invalidated = true;

        public LazyReload(Func<T> func)
        {
            _func = func;
            _thread = new Thread(Worker);
            _thread.Start();
        }

        public T Get()
        {
            if (_instance != null)
            {
                return _instance;
            }

            lock (_mutex)
            {
                while (_instance == null)
                {
                    Monitor.Wait(_mutex);
                }
            }
            return _instance;
        }

        public void Invalidate()
        {
            lock (_mutex)
            {
                if (_invalidated == true)
                {
                    return;
                }

                _invalidated = true;
                Monitor.PulseAll(_mutex);
            }
        }

        private void Worker()
        {
            while (true)
            {
                lock (_mutex)
                {
                    while (_invalidated == false)
                    {
                        Monitor.Wait(_mutex);
                    }

                    Console.WriteLine("Worker: _instance = _func();");
                    _instance = _func();
                    _invalidated = false;
                    Monitor.PulseAll(_mutex);
                }
            }
        }
    }
}