using System.Threading.Tasks;
using ESPlus.Aggregates;

namespace ESPlus.Interfaces
{
    public interface IRepository
    {
        Task SaveAsync<TAggregate>(TAggregate aggregate) where TAggregate : ReplayableObject;
        Task AppendAsync<TAggregate>(TAggregate aggregate) where TAggregate : AppendableObject;
        Task<TAggregate> GetByIdAsync<TAggregate>(string id, int version = int.MaxValue) where TAggregate : ReplayableObject;
    }
}