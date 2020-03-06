using System;
using System.Collections.Generic;
using ESPlus.Interfaces;
using ESPlus.Misc;

namespace ESPlus.Aggregates
{
    public class ProcessManager<TFirstEvent> : AggregateBase<TFirstEvent>
    {
//        private bool _dead = false;
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
            (
                corrolationId: Id,
                eventId: eventId
            ));
            
            return true;
        }

        public virtual void Wakeup(string token)
        {
        }

        protected void Dispatch<T>(T command)
        {
            ApplyChange(new ProcessManagerCommand
            (
                corrolationId: Id,
                type: typeof (T).FullName,
                payload: command
            ));
        }

        protected void SetAlarm(TimeSpan timeSpan, string token)
        {
            SetAlarm(SystemTime.UtcNow.Add(timeSpan), token);
        }

        protected void SetAlarm(DateTime alarm, string token)
        {
            ApplyChange(new ProcessManagerAlarm
            (
                corrolationId: Id,
                alarm: alarm,
                token: token
            ));
        }

        protected void PoisonPill()
        {
            ApplyChange(new ProcessManagerPoisonPill
            (
                corrolationId: Id
            ));
        }

        private void Apply(ProcessManagerPoisonPill @event)
        {
//            _dead = true;
        }

        private void Apply(ProcessManagerAlarm @event)
        {
        }        
    }
}