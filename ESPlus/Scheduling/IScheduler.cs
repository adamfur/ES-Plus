using System;
using System.Collections.Generic;

namespace ESPlus.Scheduling
{
    public interface IScheduler
    {
        void AddTrigger(Guid id, string cron, Action action);
        void Remove(Guid id);
        void Stop(Guid id);
        void Fire(Guid id);
        IEnumerable<Job> Jobs();
    }

}
