using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ESPlus.Wyrm;

namespace ESPlus.MoonGoose
{
    public interface IMoonGooseDriver
    {
        Task<byte[]> GetAsync(string database, string key, CancellationToken cancellationToken = default);
        Task PutAsync(string database, IEnumerable<Document> documents, CancellationToken cancellationToken = default);
        IAsyncEnumerable<byte[]> SearchAsync(string database, long[] parameters, CancellationToken cancellationToken = default);
    }
}