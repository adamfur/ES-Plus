using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ESPlus.Interfaces;

namespace ESPlus.Storage
{
    public class InMemoryStorage : IStorage
    {
        private readonly Dictionary<StringPair, object> _data = new Dictionary<StringPair, object>();

        public void Delete(string path, string tenant)
        {
            var key = new StringPair(path, tenant);
            
            _data.Remove(key);
        }

        public Task FlushAsync()
        {
            return Task.CompletedTask;
        }

        public Task<T> GetAsync<T>(string path, string tenant)
        {
            var key = new StringPair(path, tenant);
            
            if (_data.TryGetValue(key , out var data))
            {
                return Task.FromResult((T) data);
            }
            
            return default;
        }

        public void Put<T>(string path, string tenant, T item)
        {
            var key = new StringPair(path, tenant);
            _data[key] = item;
        }

        public void Reset()
        {
            _data.Clear();
        }

        public IAsyncEnumerable<byte[]> SearchAsync(long[] parameters, string tenant)
        {
            return null;
        }
    }
}