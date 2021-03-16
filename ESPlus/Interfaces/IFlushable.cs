using System.Threading;
using System.Threading.Tasks;

namespace ESPlus.Storage
{
    public interface IFlushable
    {
        Task FlushAsync(CancellationToken cancellationToken);
    }
}