using System.Collections.Generic;
using System.Threading.Tasks;
using ESPlus.Storage;

namespace ESPlus.Interfaces
{
    public interface IStorage : IFlushable
    {
        void Put<T>(string path, string tenant, T item);
        void Delete(string path, string tenant);
        Task<T> GetAsync<T>(string path, string tenant);
        void Reset();
        IAsyncEnumerable<byte[]> SearchAsync(long[] parameters, string tenant);
    }
}