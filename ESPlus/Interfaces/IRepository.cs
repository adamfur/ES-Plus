using System.Threading.Tasks;
using ESPlus.Aggregates;
using ESPlus.Wyrm;

namespace ESPlus.Interfaces
{
    public interface IRepository
    {
        Task<WyrmResult> SaveAsync(AggregateBase aggregate, object headers = null,
            long savePolicy = ExpectedVersion.Specified, bool encrypt = true,
            Wyrm.CommitPolicy commitPolicy = Wyrm.CommitPolicy.All);
        Task<TAggregate> GetByIdAsync<TAggregate>(string id) where TAggregate : IAggregate;
        Task<WyrmResult> CreateStreamAsync(string streamName);
        Task<WyrmResult> DeleteStreamAsync(string streamName, long version = -1);
        IRepositoryTransaction BeginTransaction();
    }
}