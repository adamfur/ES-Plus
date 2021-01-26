using System.Collections.Generic;

namespace ESPlus.Interfaces
{
    public class StorageCache : IStorage
    {
        private readonly IStorage _storage;
        private Dictionary<string, object> _cache = new Dictionary<string, object>();
        private readonly Dictionary<string, object> _staging = new Dictionary<string, object>();

        public StorageCache(IStorage storage)
        {
            _storage = storage;
        }
        
        public void Flush()
        {
            _cache = new Dictionary<string, object>(_staging);
            _staging.Clear();
            _storage.Flush();
        }

        public void Put<T>(string path, T item)
        {
            _staging[path] = item;
            _storage.Put(path, item);
        }

        public void Delete(string path)
        {
            _staging.Remove(path);
            _storage.Delete(path);
        }

        public T Get<T>(string path)
        {
            if (_cache.TryGetValue(path, out var resolved))
            {
                return (T) resolved;
            }

            return _storage.Get<T>(path);
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