using System;
using System.Collections.Generic;
using ESPlus.Interfaces;
using ESPlus.Misc;

namespace ESPlus.Aggregates
{
    public class ProcessManager : AggregateBase
    {
        private bool _dead = false;
        private ISet<string> _processed = new HashSet<string>();
        public IRepository Repository { get; set; }

        public ProcessManager(string id)
            : base(id)
        {
        }

        public bool AlreadyProcessed(string eventId)
        {
            throw new NotImplementedException();
            if (_processed.Contains(eventId))
            {
                return false;
            }

            ApplyChange(new ProcessManagerProcessed
            {
                CorrolationId = Id,
                EventId = eventId
            });
            
            return true;
        }

        public virtual void Wakeup(string token)
        {
        }

        protected void Dispatch<T>(T command)
        {
            ApplyChange(new ProcessManagerCommand
            {
                CorrolationId = Id,
                Type = typeof (T).FullName,
                Payload = command
            });
        }

        protected void SetAlarm(TimeSpan timeSpan, string token)
        {
            SetAlarm(SystemTime.UtcNow.Add(timeSpan), token);
        }

        protected void SetAlarm(DateTime alarm, string token)
        {
            ApplyChange(new ProcessManagerAlarm
            {
                CorrolationId = Id,
                Alarm = alarm,
                Token = token
            });
        }

        protected void PoisonPill()
        {
            ApplyChange(new ProcessManagerPoisonPill
            {
                CorrolationId = Id
            });
        }

        private void Apply(ProcessManagerPoisonPill @event)
        {
            _dead = true;
        }

        private void Apply(ProcessManagerAlarm @event)
        {
        }        
    }
}