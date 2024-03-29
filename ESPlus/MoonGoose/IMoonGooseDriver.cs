using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ESPlus.Wyrm;

namespace ESPlus.MoonGoose
{
    public interface IMoonGooseDriver
    {
        Task<byte[]> GetAsync(string database, string tenant, string path, CancellationToken cancellationToken = default);
        Task PutAsync(string database, List<Document> documents, Position previous, Position checkpoint, CancellationToken cancellationToken = default);
        IAsyncEnumerable<byte[]> SearchAsync(string database, string tenant, long[] parameters,
            int skip, int take, CancellationToken cancellationToken = default);
        Task<Position> ChecksumAsync(string database, CancellationToken cancellationToken);
        IAsyncEnumerable<byte[]> ListAsync(string database, string tenant, int size, int no, Box<int> total, CancellationToken cancellationToken = default);
    }
}