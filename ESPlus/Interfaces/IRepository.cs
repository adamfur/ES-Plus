using System.Threading.Tasks;
using ESPlus.Aggregates;

namespace ESPlus.Interfaces
{
    public interface IRepository
    {
        Task<Position> SaveAsync(AggregateBase aggregate, object headers = null);
        Task<Position> AppendAsync(AggregateBase aggregate, object headers = null);
        Task<Position> SaveNewAsync(IAggregate aggregate, object headers = null);
        Task<TAggregate> GetByIdAsync<TAggregate>(string id, long version = long.MaxValue) where TAggregate : IAggregate;
        Task DeleteAsync(string streamName, long version = -1);
        IRepositoryTransaction BeginTransaction();
        Task<Position> Commit();
    }
}
