using System.Collections.Generic;
using System.Threading.Tasks;

namespace ESPlus.Interfaces
{
    public interface IRepository : IStore
    {
        IRepositoryTransaction BeginTransaction();
        Task<TAggregate> GetByIdAsync<TAggregate>(string id) where TAggregate : IAggregate;

        IEnumerable<TAggregate> GetAllByTypeAsync<TAggregate>()
            where TAggregate : IAggregate;
    }
}