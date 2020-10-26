using ESPlus.Aggregates;

namespace ESPlus.SpecificationTests
{
    public class DummyAggregate : AggregateBase
    {
        public DummyAggregate(string id)
            : base(id)
        {
            ApplyChange(new DummyCreated(id));
        }

        public void Touch(int no)
        {
            ApplyChange(new DummyTouch(no));
        }

        protected void Apply(DummyCreated @event)
        {
        }        

        protected void Apply(DummyTouch @event)
        {
        }
    }

    public class DummyCreated
    {
        public string Id { get; }

        public DummyCreated(string id)
        {
            Id = id;
        }
    }

    public class DummyTouch
    {
        public int No { get; }

        public DummyTouch(int no)
        {
            No = no;
        }
    }
}