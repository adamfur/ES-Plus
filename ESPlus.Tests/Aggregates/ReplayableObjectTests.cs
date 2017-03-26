using System;
using System.Linq;
using ESPlus.Aggregates;
using ESPlus.Interfaces;
using Xunit;

namespace ESPlus.Tests.Aggregates
{
    public class ReplayableObjectTests
    {
        public class DummyEvent
        {
            public string Text { get; set; }
        }

        public class DummyAggreagate : ReplayableObject
        {
            public string Text { get; set; }

            public DummyAggreagate(string id)
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

        public ReplayableObjectTests()
        {
            _id = Guid.NewGuid().ToString();
        }

        [Fact]
        public void Version_Initialized_VerionIs0()
        {
            var aggregate = new ReplayableObject(_id);

            Assert.Equal(0, aggregate.Version);
        }

        [Fact]
        public void Id_Initialized_IdIsExpected()
        {
            var aggregate = new ReplayableObject(_id);

            Assert.Equal(_id, aggregate.Id);
        }

        [Fact]
        public void ApplyChange_NewEvent_VersionIsIncreased()
        {
            var aggregate = new ReplayableObject(_id);

            ((IAggregate)aggregate).ApplyChange(new object());
            Assert.Equal(1, aggregate.Version);
        }

        [Fact]
        public void TakeUncommitedEvents_TakeNewEvents_ContainsChangedEvents()
        {
            var aggregate = new ReplayableObject(_id);
            var event1 = new object();
            var event2 = new object();

            ((IAggregate)aggregate).ApplyChange(event1);
            ((IAggregate)aggregate).ApplyChange(event2);
            var events = ((IAggregate)aggregate).TakeUncommitedEvents().ToList();

            Assert.Equal(2, events.Count);
            Assert.Equal(event1, events[0]);
            Assert.Equal(event2, events[1]);
        }

        [Fact]
        public void TakeUncommitedEvents_TakeTwice_EventListIsEmpty()
        {
            var aggregate = new ReplayableObject(_id);
            var @event = new object();

            ((IAggregate)aggregate).ApplyChange(@event);
            ((IAggregate)aggregate).TakeUncommitedEvents();
            var events = ((IAggregate)aggregate).TakeUncommitedEvents();

            Assert.Equal(0, events.Count());
        }

        [Fact]
        public void ApplyChange_TriggerEvent_DoSomething()
        {
            var aggregate = new DummyAggreagate(_id);
            var text = Guid.NewGuid().ToString();

            aggregate.Trigger(text);

            Assert.Equal(text, aggregate.Text);
        }
    }
}