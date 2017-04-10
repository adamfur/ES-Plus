using ESPlus.Aggregates;

namespace ESPlus.Specification
{
    public class DummyEvent
    {
        public int Value { get; set; }
    }

    public class Dummy : ReplayableObject
    {
        public Dummy(string id)
            : base(id)
        {
        }

        public void Foo()
        {
            //throw new NotImplementedException();
            ApplyChange(new DummyEvent());
        }

        public void Apply(DummyEvent @event)
        {
        }
    }

    public class Demo : Specification<Dummy>
    {
        protected override Dummy Create()
        {
            return new Dummy("abc");
        }

        protected override void When()
        {
            Aggregate.Foo();
        }

        [Then]
        public void Create_generates_create_event()
        {
            Is<DummyEvent>(e => true);
            //Is<object>();
            //Is<object>();
            //Throws<NotImplementedException>(p => p.Message == "hello");
        }
    }
}