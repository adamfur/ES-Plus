using System;
using ESPlus.Aggregates;
using ESPlus.Tests.Repositories.Aggregates.Events;

namespace ESPlus.Tests.Repositories.Aggregates
{
    public class DummyAggregate : AggregateBase
    {
        public Guid Guid { get; set; }
        public int Count { get; private set; }

        public DummyAggregate(string id)
            : base(id)
        {
        }
        
        public DummyAggregate(string id, int dummy)
            : base(id)
        {
            Poke();
        }

        public void Poke()
        {
            ApplyChange(new Events.DummyEvent());
        }

        public void AttachFile()
        {
            ApplyChange(new FileMetadataAddedEvent());
            ApplyChange(new FileAddedEvent());
        }

        public void AddGuid(Guid guid)
        {
            ApplyChange(new GuidAddedEvent
            {
                Guid = guid
            });
        }

        protected void Apply(GuidAddedEvent @event)
        {
            ++Count;
            Guid = @event.Guid;
        }            

        protected void Apply(Events.DummyEvent @event)
        {
            ++Count;
        }

        [NoReplay]
        protected void Apply(FileAddedEvent @event)
        {
            ++Count;
        }

        protected void Apply(FileMetadataAddedEvent @event)
        {
            ++Count;
        }
    }
}