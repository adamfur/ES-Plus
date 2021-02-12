using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ESPlus.Wyrm;

namespace ESPlus.MoonGoose
{
    public interface IMoonGooseDriver
    {
        Task<byte[]> GetAsync(string database, string tenant, string path, CancellationToken cancellationToken = default);
        Task PutAsync(string database, List<Document> documents, CancellationToken cancellationToken = default);
        IAsyncEnumerable<byte[]> SearchAsync(string database, string tenant, long[] parameters,
            CancellationToken cancellationToken = default);
    }
}