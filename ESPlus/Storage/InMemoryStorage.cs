using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ESPlus.Interfaces;
using ESPlus.MoonGoose;

namespace ESPlus.Storage
{
    public class InMemoryStorage : IStorage
    {
        private readonly Dictionary<StringPair, object> _data = new Dictionary<StringPair, object>();

        public void Delete(string tenant, string path)
        {
            var key = new StringPair(tenant, path);
            
            _data.Remove(key);
        }

        public Task FlushAsync(Position previousCheckpoint, Position checkpoint, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task<T> GetAsync<T>(string tenant, string path, CancellationToken cancellationToken)
        {
            var key = new StringPair(tenant, path);
            
            if (_data.TryGetValue(key , out var data))
            {
                return Task.FromResult((T) data);
            }
            
            throw new StorageNotFoundException();
        }

        public void Put<T>(string tenant, string path, T item)
        {
            var key = new StringPair(tenant, path);
            _data[key] = item;
        }

        public void Reset()
        {
            _data.Clear();
        }

        public IAsyncEnumerable<byte[]> SearchAsync(string tenant, long[] parameters,
            CancellationToken cancellationToken)
        {
            return null;
        }

        public Task<Position> ChecksumAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(Position.Start);
        }

        public IAsyncEnumerable<byte[]> List<T>(string tenant, int size, int no, Box<int> total, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task EvictCache()
        {
            return Task.CompletedTask;
        }
    }
}