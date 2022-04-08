using System.Threading;
using System.Threading.Tasks;
using ESPlus.Wyrm;

namespace ESPlus.Interfaces
{
    public interface IGenericWyrmRepository
    {
        Task<WyrmResult> SaveAsync<T>(IAggregate<T> aggregate, object headers, CancellationToken cancellationToken = default);
        Task<TAggregate> GetByIdAsync<TAggregate, T>(T id, CancellationToken cancellationToken = default, long version = long.MaxValue)
            where TAggregate : IAggregate<T>;
        Task DeleteAsync<T>(T id, long version = -1, CancellationToken cancellationToken = default);
    }
}