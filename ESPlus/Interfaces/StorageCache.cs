using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace ESPlus.Interfaces
{
    public class StorageCache : IStorage
    {
        private readonly IStorage _storage;
        private readonly ConditionalWeakTable<StringPair, object> _cache = new ConditionalWeakTable<StringPair, object>();

        public StorageCache(IStorage storage)
        {
            _storage = storage;
        }
        
        public async Task FlushAsync()
        {
            await _storage.FlushAsync();
        }

        public void Put<T>(string path, string tenant, T item)
        {
            _cache.AddOrUpdate(new StringPair(tenant, path), item);
            _storage.Put(path, tenant, item);
        }

        public void Delete(string path, string tenant)
        {
            _cache.Remove(new StringPair(tenant, path));
            _storage.Delete(path, tenant);
        }

        public async Task<T> GetAsync<T>(string path, string tenant)
        {
            var key = new StringPair(tenant, path);
            
            if (_cache.TryGetValue(key, out var resolved))
            {
                return (T) resolved;
            }

            var item = await _storage.GetAsync<T>(path, tenant);
            
            _cache.AddOrUpdate(key, item);
            
            return item;
        }

        public void Reset()
        {
            _storage.Reset();
        }

        public IAsyncEnumerable<byte[]> SearchAsync(long[] parameters, string tenant)
        {
            return _storage.SearchAsync(parameters, tenant);
        }
    }
}