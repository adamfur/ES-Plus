using System.Collections.Generic;
using System.Threading.Tasks;
using ESPlus.Wyrm;

namespace ESPlus.Interfaces
{
    public class MuteTransaction : IRepositoryTransaction
    {
        public void Dispose()
        {
        }

        public IEnumerable<WyrmEvent> Events { get; }
        
        public Task<Position> Commit(CommitPolicy policy)
        {
            return Task.FromResult(Position.Start);
        }

        public void Append(IEnumerable<WyrmEvent> events)
        {
        }
    }
}