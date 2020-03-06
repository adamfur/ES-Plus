using System.Threading.Tasks;
using ESPlus.Wyrm;

namespace ESPlus.Interfaces
{
    public interface IStore
    {
        Task<WyrmResult> SaveAsync(IAggregate aggregate, object headers = null,
            long expectedVersion = ExpectedVersion.Specified, bool encrypt = true);
        Task<WyrmResult> CreateStreamAsync(string streamName);
        Task<WyrmResult> DeleteStreamAsync(string streamName, long version = -1);

        // Task<WyrmResult> SetAlarm(string streamName, string token, DateTime trigger);
        // Task<WyrmResult> CancelAlarm(string streamName, string token);
    }
}