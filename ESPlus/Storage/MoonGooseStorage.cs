using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ESPlus.Interfaces;
using ESPlus.MoonGoose;

namespace ESPlus.Storage
{
    public class MoonGooseStorage : IStorage
    {
        private readonly IMoonGooseDriver _driver;
        private readonly string _collection;
        protected readonly Dictionary<StringPair, Document> Writes = new Dictionary<StringPair, Document>();

        public MoonGooseStorage(IMoonGooseDriver driver, string collection)
        {
            _driver = driver;
            _collection = collection;
        }

        public async Task FlushAsync(Position previousCheckpoint, Position checkpoint, CancellationToken cancellationToken)
        {
            await Task.Run(() => Retry.RetryAsync(() => _driver.PutAsync(_collection, Writes.Values.ToList(), previousCheckpoint, checkpoint, cancellationToken), cancellationToken), cancellationToken);
            Writes.Clear();
        }

        public async Task<T> GetAsync<T>(string tenant, string path, CancellationToken cancellationToken)
        {
            var key = new StringPair(tenant, path);
            
            if (Writes.TryGetValue(key, out var resolved))
            {
                if (resolved.Operation == Operation.Save)
                {
                    return (T) resolved.Item;
                }
                
                throw new StorageNotFoundException(key.ToString());
            }

            try
            {
                var payload = await _driver.GetAsync(_collection, key.Tenant, key.Path, cancellationToken);

                return JsonSerializer.Deserialize<T>(payload);
            }
            catch (MoonGooseNotFoundException ex)
            {
                throw new StorageNotFoundException(ex.Message, ex);
            }
        }

        public virtual void Put<T>(string tenant, string path, T item)
        {
            var key = new StringPair(tenant, path);
            
            Writes[key] = new Document(key.Tenant, key.Path, item, Operation.Save);
        }

        public void Delete(string tenant, string path)
        {
            var key = new StringPair(tenant, path);
            
            Writes[key] = new Document(key.Tenant, key.Path, null, Operation.Delete);
        }

        public void Reset()
        {
        }

        public IAsyncEnumerable<byte[]> SearchAsync(string tenant, long[] parameters,
            CancellationToken cancellationToken)
        {
            return _driver.SearchAsync(_collection, tenant, parameters, 0, 100, cancellationToken);
        }
        
        public Task<Position> ChecksumAsync(CancellationToken cancellationToken)
        {
            return _driver.ChecksumAsync(_collection, cancellationToken);
        }
    }
}      
