using System;

namespace ESPlus.Aggregates
{
    public class ProcessManager : AggregateBase
    {
        private bool _dead = false;

        public ProcessManager(string id)
            : base(id)
        {
        }

        // public void Dispatch<T>(T command)
        // {
        //     this.ApplyChange(new ProcessManagerCommand
        //     {
        //         Type = typeof (T).FullName,
        //         Payload = command
        //     });
        // }

        // public void SetAlarm(TimeSpan timeSpan)
        // {
        //     SetAlarm(SystemTime.UtcNow.Add(timeSpan));
        // }

        // public void SetAlarm(DateTime alarm)
        // {
        //     ApplyChange(new ProcessManagerAlarm
        //     {
        //         Id = Id,
        //         Alarm = alarm
        //     });
        // }

        // public void PoisonPill()
        // {
        //     ApplyChange(new ProcessManagerPoisonPill
        //     {
        //         Id = Id
        //     });
        // }

        private void Apply(ProcessManagerPoisonPill @event)
        {
            _dead = true;
        }
    }

    public class ProcessManagerPoisonPill
    {
        public string Id { get; set; }
    }

    public class ProcessManagerAlarm
    {
        public string Id { get; set; }
        public DateTime Alarm { get; set; }
    }
}