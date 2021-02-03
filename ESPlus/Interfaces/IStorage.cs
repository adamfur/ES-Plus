using System.Collections.Generic;
using System.Threading.Tasks;
using ESPlus.Storage;

namespace ESPlus.Interfaces
{
    public interface IStorage : IFlushable
    {
        void Put<T>(string path, T item);
        void Delete(string path);
        Task<T> GetAsync<T>(string path);
        void Reset();
        IAsyncEnumerable<byte[]> SearchAsync(long[] parameters);
    }
}