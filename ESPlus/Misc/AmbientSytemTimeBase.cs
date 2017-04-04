using System;
using System.Threading;

namespace ESPlus
{
    public abstract class AmbientSytemTimeBase
    {
        protected static readonly AsyncLocal<Func<DateTime>> AsyncLocal = new AsyncLocal<Func<DateTime>>();
    }
}