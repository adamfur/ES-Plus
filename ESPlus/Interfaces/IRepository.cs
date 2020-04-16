using System;
using System.Threading.Tasks;
using ESPlus.Aggregates;
using ESPlus.Wyrm;

namespace ESPlus.Interfaces
{
    public interface IRepository
    {
        Task<WyrmResult> SaveAsync(AggregateBase aggregate, object headers = null);
        Task<WyrmResult> AppendAsync(AggregateBase aggregate, object headers = null);
        Task<Position> SaveNewAsync(IAggregate aggregate, object headers = null);
        Task<TAggregate> GetByIdAsync<TAggregate>(string id, long version = long.MaxValue) where TAggregate : IAggregate;
        Task DeleteAsync(string streamName, long version = -1);
        IRepositoryTransaction BeginTransaction();
        Task<WyrmResult> Commit();
        void Observe(Action<object> @event);
    }
}
