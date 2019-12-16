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
        
        public Task<WyrmResult> Commit()
        {
            return Task.FromResult(new WyrmResult(Position.Start, 0));
        }

        public void Append(IEnumerable<WyrmEvent> events)
        {
        }
    }
}