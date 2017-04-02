using ESPlus.Aggregates;

namespace ESPlus.Interfaces
{
    public interface IRepository
    {
        void Save<TAggregate>(TAggregate aggregate) where TAggregate : ReplayableObject;
        void Append<TAggregate>(TAggregate aggregate) where TAggregate : AppendableObject;
        TAggregate GetById<TAggregate>(string id, int version = int.MaxValue) where TAggregate : ReplayableObject;
    }
}