using System.Collections.Generic;
using System.Threading.Tasks;
using ESPlus.Aggregates;
using ESPlus.Wyrm;

namespace ESPlus.Interfaces
{
    public class MuteTransaction : IRepositoryTransaction
    {
        public void Dispose()
        {
        }

        public IEnumerable<WyrmEvent> Events { get; }
        
        public async Task<WyrmResult> Commit(CommitPolicy policy = CommitPolicy.All)
        {
            return WyrmResult.Empty();
        }

        public void Append(IEnumerable<WyrmEvent> events)
        {
        }

        public Task<WyrmResult> SaveAsync(AggregateBase aggregate, object headers = null, long savePolicy = ExpectedVersion.Specified,
            bool encrypt = true)
        {
            throw new System.NotImplementedException();
        }

        public Task<TAggregate> GetByIdAsync<TAggregate>(string id) where TAggregate : IAggregate
        {
            throw new System.NotImplementedException();
        }

        public Task<WyrmResult> CreateStreamAsync(string streamName)
        {
            throw new System.NotImplementedException();
        }

        public Task<WyrmResult> DeleteStreamAsync(string streamName, long version = -1)
        {
            throw new System.NotImplementedException();
        }
    }
}