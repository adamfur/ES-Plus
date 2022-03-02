using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ESPlus.Storage;

namespace ESPlus.Interfaces
{
    public interface IStorage : IFlushable
    {
        void Put<T>(string tenant, string path, T item);
        void Delete(string tenant, string path);
        Task<T> GetAsync<T>(string tenant, string path, CancellationToken cancellationToken);
        void Reset();
        IAsyncEnumerable<byte[]> SearchAsync(string tenant, long[] parameters, CancellationToken cancellationToken);
        Task<Position> ChecksumAsync(CancellationToken cancellationToken);
        IAsyncEnumerable<byte[]> List(string tenant, int size, int no, CancellationToken cancellationToken);
    }
}