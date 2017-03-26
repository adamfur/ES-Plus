using System.Collections.Generic;
using ESPlus.Interfaces;

namespace ESPlus.Storage
{
    public class InMemoryStorage : IStorage
    {
        private readonly Dictionary<string, object> _cache = new Dictionary<string, object>();

        public void Flush()
        {
        }

        public T Get<T>(string path)
        {
            if (_cache.ContainsKey(path))
            {
                return (T) _cache[path];
            }
            return default (T);
        }

        public void Put(string path, object item)
        {
            _cache[path] = item;
        }
    }
}