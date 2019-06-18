using System.Threading.Tasks;
using ESPlus.Aggregates;

namespace ESPlus.Interfaces
{
    public interface IRepository
    {
        Task<byte[]> SaveAsync(AggregateBase aggregate, object headers = null);
        Task<byte[]> AppendAsync(AggregateBase aggregate, object headers = null);
        Task SaveNewAsync(IAggregate aggregate, object headers = null);
        Task<TAggregate> GetByIdAsync<TAggregate>(string id, long version = long.MaxValue) where TAggregate : IAggregate;
        Task DeleteAsync(string streamName, long version = -1);
        RepositoryTransaction BeginTransaction();
        Task<byte[]> Commit();
    }
}
