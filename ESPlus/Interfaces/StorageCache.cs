using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
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
        
        public async Task FlushAsync(CancellationToken cancellationToken)
        {
            await _storage.FlushAsync(cancellationToken);
        }

        public void Put<T>(string tenant, string path, T item)
        {
            _cache.AddOrUpdate(new StringPair(tenant, path), item);
            _storage.Put(tenant, path, item);
        }

        public void Delete(string tenant, string path)
        {
            _cache.Remove(new StringPair(tenant, path));
            _storage.Delete(tenant, path);
        }

        public async Task<T> GetAsync<T>(string tenant, string path, CancellationToken cancellationToken)
        {
            var key = new StringPair(tenant, path);
            
            if (_cache.TryGetValue(key, out var resolved))
            {
                return (T) resolved;
            }

            var item = await _storage.GetAsync<T>(tenant, path, cancellationToken);
            
            _cache.AddOrUpdate(key, item);
            
            return item;
        }

        public void Reset()
        {
            _storage.Reset();
        }

        public IAsyncEnumerable<byte[]> SearchAsync(string tenant, long[] parameters,
            CancellationToken cancellationToken)
        {
            return _storage.SearchAsync(tenant, parameters, cancellationToken);
        }
    }
}