using System;
using System.Linq;
using ESPlus.Aggregates;
using ESPlus.Interfaces;
using Xunit;

namespace ESPlus.Tests.Aggregates
{
    public class AggregateBaseTests
    {
        public class DummyEvent
        {
            public string Text { get; set; }
        }

        public class DummyAggregate : AggregateBase
        {
            public string Text { get; set; }

            public DummyAggregate(string id)
                : base(id)
            {
            }

            public void Trigger(string text)
            {
                ApplyChange(new DummyEvent
                {
                    Text = text
                });
            }

            private void Apply(DummyEvent @event)
            {
                Text = @event.Text;
            }
        }

        private readonly string _id;

        public AggregateBaseTests()
        {
            _id = Guid.NewGuid().ToString();
        }

        [Fact]
        public void Version_Initialized_VersionIs0()
        {
            var aggregate = new DummyAggregate(_id);

            Assert.Equal(-1, aggregate.Version);
        }

        [Fact]
        public void Id_Initialized_IdIsExpected()
        {
            var aggregate = new DummyAggregate(_id);

            Assert.Equal(_id, aggregate.Id);
        }

        [Fact]
        public void ApplyChange_NewEvent_VersionIsIncreased()
        {
            var aggregate = new DummyAggregate(_id) as IAggregate;

            aggregate.ApplyChange(new object());
            Assert.Equal(0, aggregate.Version);
        }

        [Fact]
        public void TakeUncommittedEvents_TakeNewEvents_ContainsChangedEvents()
        {
            var aggregate = new DummyAggregate(_id) as IAggregate;
            var event1 = new object();
            var event2 = new object();

            aggregate.ApplyChange(event1);
            aggregate.ApplyChange(event2);
            var events = aggregate.TakeUncommittedEvents().ToList();

            Assert.Equal(2, events.Count);
            Assert.Equal(event1, events[0]);
            Assert.Equal(event2, events[1]);
        }

        [Fact]
        public void TakeUncommittedEvents_TakeTwice_EventListIsEmpty()
        {
            var aggregate = new DummyAggregate(_id) as IAggregate;
            var @event = new object();

            aggregate.ApplyChange(@event);
            aggregate.TakeUncommittedEvents();
            var events = aggregate.TakeUncommittedEvents();

            Assert.Empty(events);
        }

        [Fact]
        public void ApplyChange_TriggerEvent_DoSomething()
        {
            var aggregate = new DummyAggregate(_id);
            var text = Guid.NewGuid().ToString();

            aggregate.Trigger(text);

            Assert.Equal(text, aggregate.Text);
        }
    }
}