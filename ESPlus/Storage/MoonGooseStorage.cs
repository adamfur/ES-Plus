using System;
using System.Collections.Generic;
using System.Data.HashFunction.xxHash;
using System.Linq;
using System.Text;
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
        protected readonly Dictionary<string, Document> _writes = new Dictionary<string, Document>();
        protected readonly IxxHash _algorithm;

        public MoonGooseStorage(IMoonGooseDriver driver, string collection)
        {
            _driver = driver;
            _collection = collection;
            _algorithm = xxHashFactory.Instance.Create(new xxHashConfig { HashSizeInBits = 64 });
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

            _writes.Clear();
        }

        private IEnumerable<Document> Assemble()
        {
            return _writes.Values;
        }

        public async Task<T> GetAsync<T>(string path, string tenant)
        {
            var key = path ?? "@";
            
            if (_writes.TryGetValue(key, out var resolved))
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

            var payload = await _driver.GetAsync(_collection, tenant, key);

            return JsonSerializer.Deserialize<T>(payload);
        }

        public virtual void Put<T>(string path, string tenant, T item)
        {
            var key = path ?? "@";
            var keywords = new long[0];
            Flags flags = Flags.None;
            var json = JsonSerializer.Serialize(item);
            var encoded = Encoding.UTF8.GetBytes(json);

            _writes[key] = new Document(key, keywords, encoded, item, tenant, flags, Operation.Save);
        }

        public void Delete(string path, string tenant)
        {
            var key = path ?? "@";
            
            _writes[key] = new Document(key, new long[0], new byte[0], null, tenant, Flags.Indexed, Operation.Delete);
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
