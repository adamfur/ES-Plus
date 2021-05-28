using System;
using System.Threading;
using System.Threading.Tasks;
using ESPlus.Aggregates;
using ESPlus.Wyrm;

namespace ESPlus.Interfaces
{
    public interface IRepository
    {
        Task<WyrmResult> SaveAsync(AggregateBase aggregate, object headers = null, CancellationToken cancellationToken = default);
        Task<WyrmResult> AppendAsync(AggregateBase aggregate, object headers = null, CancellationToken cancellationToken = default);
        Task<Position> SaveNewAsync(IAggregate aggregate, object headers = null, CancellationToken cancellationToken = default);
        Task<TAggregate> GetByIdAsync<TAggregate>(string id, CancellationToken cancellationToken,
            long version = long.MaxValue) where TAggregate : IAggregate;
        Task DeleteAsync(string streamName, long version = -1, CancellationToken cancellationToken = default);
        void Observe(Action<object> @event);
    }
}
