using System.Collections.Generic;
using ESPlus.Storage;

namespace ESPlus.Interfaces
{
    public interface IStorage : IFlushable
    {
        void Put<T>(string path, T item);
        void Delete(string path);
        T Get<T>(string path);
        void Reset();
        IAsyncEnumerable<byte[]> SearchAsync(long[] parameters);
    }
}