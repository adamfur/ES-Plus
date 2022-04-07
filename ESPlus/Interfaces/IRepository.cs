using System;
using System.Threading;
using System.Threading.Tasks;
using ESPlus.Aggregates;
using ESPlus.Wyrm;

namespace ESPlus.Interfaces
{
    public interface IRepository
    {
        Task<WyrmResult> SaveAsync(AggregateBase aggregate, object headers = null,
            CancellationToken cancellationToken = default);

        Task<WyrmResult> AppendAsync(AggregateBase aggregate, object headers = null,
            CancellationToken cancellationToken = default);

        Task<Position> SaveNewAsync(IAggregate aggregate, object headers = null,
            CancellationToken cancellationToken = default);

        Task<TAggregate> GetByIdAsync<TAggregate>(string id, CancellationToken cancellationToken,
            long version = long.MaxValue) where TAggregate : IAggregate;

        Task DeleteAsync(string streamName, long version = -1, CancellationToken cancellationToken = default);
        void Observe(Action<object> @event);
    }

    public interface IGenericRepository
    {
        Task<WyrmResult> SaveAsync<T>(AggregateBase<T> aggregate, object headers = null,
            CancellationToken cancellationToken = default);

        Task<WyrmResult> AppendAsync<T>(IAggregate<T> aggregate, object headers = null, CancellationToken cancellationToken = default);

        Task<TAggregate> GetByIdAsync<TAggregate, T>(T id, CancellationToken cancellationToken = default,
            long version = long.MaxValue) where TAggregate : IAggregate<T>;

        Task DeleteAsync(string id, long version = -1, CancellationToken cancellationToken = default);
    }

}