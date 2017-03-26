using ESPlus.Aggregates;

namespace ESPlus.Interfaces
{
    public interface IRepository
    {
        void Save(ReplayableObject aggregate);
        void Save(AppendableObject aggregate);
        TAggregate GetById<TAggregate>(string id, long version = long.MaxValue) where TAggregate : ReplayableObject;
    }
}