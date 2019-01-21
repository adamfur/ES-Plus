using System;

namespace ESPlus.Lazy
{
    public class LazyReload<T>
    {
        private T _instance;
        private readonly Func<T> _func;
        private DateTime _deadline;
        private readonly TimeSpan _timeToLive;

        public LazyReload(Func<T> func, TimeSpan timeToLive)
        {
            _timeToLive = timeToLive;
            _func = func;
        }

        public T Get()
        {
            if (_instance != null)
            {
                if (_deadline < DateTime.Now)
                {
                    return _instance;
                }
            }

            _instance = _func();
            _deadline = DateTime.Now.Add(_timeToLive);
            return _instance;
        }
    }
}