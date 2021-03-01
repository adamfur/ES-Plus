using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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

        public async Task FlushAsync()
        {
            var bulk = new List<Document>();

            bulk.AddRange(Assemble());

            if (!bulk.Any())
            {
                return;
            }

            await Task.Run(() => Retry.RetryAsync(() => _driver.PutAsync(_collection, bulk)));

            Writes.Clear();
        }

        private IEnumerable<Document> Assemble()
        {
            return Writes.Values;
        }

        public async Task<T> GetAsync<T>(string path, string tenant)
        {
            var key = new StringPair(path, tenant);
            
            if (Writes.TryGetValue(key, out var resolved))
            {
                if (resolved.Operation == Operation.Save)
                {
                    return (T) resolved.Item;
                }
                else
                {
                    return default;
                }
            }

            try
            {
                var payload = await _driver.GetAsync(_collection, tenant, path);

                return JsonSerializer.Deserialize<T>(payload);
            }
            catch (MoonGooseNotFoundException ex)
            {
                throw new StorageNotFoundException(ex.Message, ex);
            }
        }

        public virtual void Put<T>(string path, string tenant, T item)
        {
            var key = new StringPair(path, tenant);
            
            Writes[key] = new Document(path, tenant, item, Operation.Save);
        }

        public void Delete(string path, string tenant)
        {
            var key = new StringPair(path, tenant);
            
            Writes[key] = new Document(path, tenant, null, Operation.Delete);
        }

        public void Reset()
        {
        }

        public IAsyncEnumerable<byte[]> SearchAsync(long[] parameters, string tenant)
        {
            return _driver.SearchAsync(_collection, tenant, parameters);
        }
    }
}      
