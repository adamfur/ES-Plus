using System;
using Cron;

namespace ESPlus.Scheduling
{
    public class Job
    {
        public Guid Id { get; set; }
        public DateTime? Next { get; set; }
        public TimeSpan? Left => Next - Cron.SystemTime.UtcNow;
        public DateTime? Last { get; set; }
        public DateTime? Passed { get; set; }
        public Action Action { get; set; }
        public ICronScheduler Scheduler { get; set; }

        public void Fire()
        {
            Action();
            Next = Scheduler.Next();
            Passed = Cron.SystemTime.UtcNow;
        }

        public void Stop()
        {
            Next = null;
        }
    }

}



// using Quartz;
// using Quartz.Impl;

// namespace ESPlus.Scheduling
// {
//     public interface IScheduler
//     {
//         void AddTrigger(string cron, Action action);
//         void Start();
//     }

//     public class Scheduler : IScheduler
//     {
//         private Quartz.IScheduler _scheduler;

//         public void AddTrigger(string cron, Action action)
//         {
//             var job = JobBuilder.Create<ActionJob>()
//                 .WithIdentity(Guid.NewGuid().ToString(), "group1")
//                 .SetJobData(new JobDataMap
//                     {
//                         {"action", action}
//                     })
//                 .Build();

//             var trigger = TriggerBuilder.Create()
//                 .WithIdentity(Guid.NewGuid().ToString(), "group1")
//                 .StartNow()
//                 .WithCronSchedule(cron)
//                 .Build();

//             _scheduler.ScheduleJob(job, trigger).Wait();
//         }

//         public void Start()
//         {
//             try
//             {
//                 var props = new NameValueCollection
//                 {
//                     //{ "quartz.serializer.type", "binary" }
//                 };
//                 var factory = new StdSchedulerFactory(props);
//                 _scheduler = factory.GetScheduler().Result;

//                 // and start it off
//                 _scheduler.Start().Wait();
//             }
//             catch (SchedulerException se)
//             {
//                 //await Console.Error.WriteLineAsync(se.ToString());
//             }
//         }
//     }

//     public class ActionJob : IJob
//     {
//         public Task Execute(IJobExecutionContext context)
//         {
//             var action = context.MergedJobDataMap["action"] as Action;
//             action();
//             return Task.WhenAll();
//         }
//     }
// }