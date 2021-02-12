using System;
using ESPlus.Aggregates;
using ESPlus.IntegrationTests.Repositories.Aggregates.Events;

namespace ESPlus.IntegrationTests.Repositories.Aggregates
{
    public class DummyAggregate : AggregateBase
    {
        public Guid Guid { get; set; }

        public DummyAggregate(string id)
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
            Guid = @event.Guid;
        }            

        protected void Apply(Events.DummyEvent @event)
        {
        }

        [NoReplay]
        protected void Apply(FileAddedEvent @event)
        {
        }

        protected void Apply(FileMetadataAddedEvent @event)
        {
        }
    }
}