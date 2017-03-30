using System;
using System.Threading;

namespace ESPlus
{
    public class AmbientSystemTimeScope : IDisposable
    {
        internal static readonly AsyncLocal<Func<DateTime>> AsyncLocal = new AsyncLocal<Func<DateTime>>();
        private Func<DateTime> _previous;

        public AmbientSystemTimeScope(Func<DateTime> method)
        {
            _previous = AsyncLocal.Value;
            AsyncLocal.Value = method;
        }

        public void Dispose()
        {
            AsyncLocal.Value = _previous;
        }
    }
}