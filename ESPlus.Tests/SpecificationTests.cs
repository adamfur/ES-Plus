using System;
using ESPlus.Aggregates;
using Xunit;

namespace ESPlus.Tests
{
    public class DummyAggregate : ReplayableObject
    {
        public DummyAggregate(bool emit)
            : base("N/A")
        {
            ApplyChange(new DummyEvent
            {
                Explanation = "Created"
            });
        }

        public void Emit()
        {
            ApplyChange(new DummyEvent
            {
                Explanation = "Emit"
            });
        }
    }

    public class DummyEvent
    {
        public string Explanation { get; set; }
    }

    public class SpecificationTests : ESPlus.Specification.Specification<DummyAggregate>
    {
        private Exception _exception;

        protected override DummyAggregate Create()
        {
            if (_exception != null)
            {
                throw _exception;
            }

            return new DummyAggregate(false);
        }

        [Fact]
        public void Constructor_Create_Emit()
        {
            Then(() =>
            {
                Is<DummyEvent>(p => p.Explanation == "Created");
            });
        }

        [Fact]
        public void Constructor_CreateBadMatch_Throws()
        {
            Assert.Throws<Exception>(() =>
            {
                Then(() =>
                {
                    Is<DummyEvent>(p => p.Explanation == "NOT Created");
                });
            });
        }

        [Fact]
        public void Constructor_EmptyWhen_Nothing()
        {
            When(() => { });

            ThenNothing();
        }

        [Fact]
        public void Constructor_Throws_Propagate()
        {
            _exception = new Exception();

            Assert.Throws<Exception>(() =>
            {
                When(() =>
                {

                });

                //ThenNothing();
            });
        }

        [Fact]
        public void When_Throws_Propagate()
        {
            Assert.Throws<Exception>(() =>
            {
                When(() =>
                {
                    throw new Exception();
                });

                ThenNothing();
            });
        }

        [Fact]
        public void Constructor_Throws_Capture()
        {
            _exception = new Exception();

            ThenThrows<Exception>();
        }

        [Fact]
        public void When_Throws_Capture()
        {
            When(() =>
            {
                throw new Exception();
            });

            ThenThrows<Exception>();
        }

        [Fact]
        public void Then_MoreEmittedThanCapturedEvents_Throws()
        {
            var exception = Assert.Throws<Exception>(() =>
            {
                When(() =>
                {
                    Aggregate.Emit();
                });

                Then(() =>
                {
                    Is<object>();
                });
            });

            //Assert.Equal("No more events!", exception.Message);
        }
    }
}