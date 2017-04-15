using System;

namespace ESPlus.Scheduling
{
    public interface IScheduler
    {
        void AddTrigger(string cron, Action action);
    }

    public class Scheduler : IScheduler
    {
        public void AddTrigger(string cron, Action action)
        {
            
        }
    }
}