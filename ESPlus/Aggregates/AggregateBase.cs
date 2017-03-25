using ESPlus.Interfaces;

namespace ESPlus.Aggregates
{
    public abstract class AggregateBase : IAggregate
    {
        protected AggregateBase(string id)
        {
            
        }

        public int Version { get; private set; } = 0;
        public string Id { get; private set; }
    }
}
