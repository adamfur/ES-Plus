using System;

namespace ESPlus
{
    public class AmbientSystemTimeScope : AmbientSytemTimeBase, IDisposable
    {
        private readonly Func<DateTime> _previous;

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