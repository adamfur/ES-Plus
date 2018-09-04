using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
// using Cron;
using ESPlus.Misc;

namespace ESPlus.Scheduling
{
    // public class Scheduler : IScheduler
    // {
    //     private List<Job> _jobs = new List<Job>();

    //     public void AddTrigger(Guid id, string cron, Action action)
    //     {
    //         var parser = new CronParser(new MonthLookupFactory());
    //         var scheduler = parser.Parse(cron);

    //         _jobs.Add(new Job
    //         {
    //             Id = id,
    //             Action = action,
    //             Scheduler = scheduler,
    //             Next = scheduler.Next()
    //         });
    //     }

    //     public void Fire(Guid id)
    //     {
    //         _jobs.Single(x => x.Id == id).Fire();
    //     }

    //     public IEnumerable<Job> Jobs()
    //     {
    //         return _jobs;
    //     }

    //     public void Remove(Guid id)
    //     {
    //         _jobs.RemoveAll(j => j.Id == id);
    //     }

    //     public void Stop(Guid id)
    //     {
    //         _jobs.Single(x => x.Id == id).Stop();
    //     }
    // }
}
