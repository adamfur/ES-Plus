using System.Threading.Tasks;
using ESPlus.Aggregates;

namespace ESPlus.Interfaces
{
    public interface IRepository
    {
        Task SaveAsync(AggregateBase aggregate);
        Task AppendAsync(AggregateBase aggregate);
        Task SaveNewAsync(IAggregate aggregate);
        Task<TAggregate> GetByIdAsync<TAggregate>(string id, int version = int.MaxValue) where TAggregate : IAggregate;
        Task DeleteAsync(string streamName, long version = 0);
    }

    public static class WritePolicy
    {
        public static long Any = -2;
        public static long NoStream = -1;
        public static long EmptyStream = -1;
        public static long StreamExists = -4;
    }
}
