using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace ESPlus.Interfaces
{
    public class StorageCache : IStorage
    {
        private readonly IStorage _storage;
        private readonly ConditionalWeakTable<string, object> _cache = new ConditionalWeakTable<string, object>();

        public StorageCache(IStorage storage)
        {
            _storage = storage;
        }
        
        public async Task FlushAsync()
        {
            await _storage.FlushAsync();
        }

        public void Put<T>(string path, T item)
        {
            _cache.AddOrUpdate(path, item);
            _storage.Put(path, item);
        }

        public void Delete(string path)
        {
            _cache.Remove(path);
            _storage.Delete(path);
        }

        public async Task<T> GetAsync<T>(string path)
        {
            if (_cache.TryGetValue(path, out var resolved))
            {
                return (T) resolved;
            }

            return await _storage.GetAsync<T>(path);
        }

        public void Reset()
        {
            _storage.Reset();
        }

        public IAsyncEnumerable<byte[]> SearchAsync(long[] parameters)
        {
            return _storage.SearchAsync(parameters);
        }
    }
}