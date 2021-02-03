using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ESPlus.Interfaces;

namespace ESPlus.Storage
{
    public class InMemoryStorage : IStorage
    {
        public readonly Dictionary<string, object> _data = new Dictionary<string, object>();

        public Dictionary<string, object> Internal => _data;

        public void Delete(string path)
        {
            _data.Remove(path);
        }

        public Task FlushAsync()
        {
            return Task.CompletedTask;
        }

        public Task<T> GetAsync<T>(string path)
        {
            Console.WriteLine(string.Join(", ", _data.Values));
            
            if (_data.TryGetValue(path, out var data))
            {
                return Task.FromResult((T) data);
            }
            
            return default;
        }

        public void Put<T>(string path, T item)
        {
            _data[path] = item;
            // Console.WriteLine($" -- PUT: {path}");
        }

        public void Reset()
        {
            _data.Clear();
        }

        public IAsyncEnumerable<byte[]> SearchAsync(long[] parameters)
        {
            throw new NotImplementedException();
        }
    }
}