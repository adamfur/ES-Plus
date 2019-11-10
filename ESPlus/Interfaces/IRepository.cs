using System.Threading.Tasks;
using ESPlus.Aggregates;
using ESPlus.Wyrm;

namespace ESPlus.Interfaces
{
    public interface IRepository
    {
        Task<Position> SaveAsync(AggregateBase aggregate,
            object headers = null, long savePolicy = ExpectedVersion.Specified);
        Task<TAggregate> GetByIdAsync<TAggregate>(string id, long version = long.MaxValue) where TAggregate : IAggregate;
        Task<Position> CreateStreamAsync(string streamName);
        Task<Position> DeleteStreamAsync(string streamName, long version = -1);
        IRepositoryTransaction BeginTransaction();
    }
}
