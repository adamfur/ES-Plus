using System.Threading.Tasks;
using ESPlus.Aggregates;
using ESPlus.Wyrm;

namespace ESPlus.Interfaces
{
    public interface IStore
    {
        Task<WyrmResult> SaveAsync(AggregateBase aggregate, object headers = null,
            long expectedVersion = ExpectedVersion.Specified, bool encrypt = true);
        Task<WyrmResult> CreateStreamAsync(string streamName);
        Task<WyrmResult> DeleteStreamAsync(string streamName, long version = -1); 
    }
}