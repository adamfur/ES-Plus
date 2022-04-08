using System.Threading;
using System.Threading.Tasks;
using ESPlus.Wyrm;

namespace ESPlus.Interfaces
{
    public interface IWyrmRepository
    {
        Task<WyrmResult> SaveAsync<TAggregate>(TAggregate aggregate, object headers, CancellationToken cancellationToken = default)
            where TAggregate : IAggregate<string>;
        Task<TAggregate> GetByIdAsync<TAggregate>(string id, CancellationToken cancellationToken = default, long version = long.MaxValue)
            where TAggregate : IAggregate<string>;
        Task DeleteAsync(string id, long version = -1, CancellationToken cancellationToken = default);
    }
}