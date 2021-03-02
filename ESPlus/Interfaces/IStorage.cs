using System.Collections.Generic;
using System.Threading.Tasks;
using ESPlus.Storage;

namespace ESPlus.Interfaces
{
    public interface IStorage : IFlushable
    {
        void Put<T>(string tenant, string path, T item);
        void Delete(string tenant, string path);
        Task<T> GetAsync<T>(string tenant, string path);
        void Reset();
        IAsyncEnumerable<byte[]> SearchAsync(string tenant, long[] parameters);
    }
}