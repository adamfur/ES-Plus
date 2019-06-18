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
    }

    public static class WritePolicy
    {
        public static long Any = -2;
        public static long NoStream = -1;
        public static long EmptyStream = -1;
        public static long StreamExists = -4;
    }
}
