using System.Collections.Generic;
using System.Linq;
using ESPlus.Interfaces;
using Raven.Client.Documents;

namespace ESPlus.Storage.Raven
{
    public class RavenStorage : IStorage
    {
        private readonly IDocumentStore _documentStore;
        private Dictionary<string, object> _writeCache = new Dictionary<string, object>();
        private Dictionary<string, object> _cache = new Dictionary<string, object>();

        public RavenStorage(IDocumentStore documentStore)
        {
            _documentStore = documentStore;
        }

        public void Flush()
        {
            for (var i = 0; ; ++i)
            {
                var page = _writeCache.Skip(i * 30).Take(30);

                if (!page.Any())
                {
                    break;
                }

                using (var session = _documentStore.OpenSession())
                {
                    foreach (var item in page)
                    {
                        session.Store(item.Value, item.Key);
                    }
                    session.SaveChanges();
                }
            }

            _writeCache.Clear();
        }

        public T Get<T>(string path)
        {
            if (_cache.ContainsKey(path))
            {
                return (T)_cache[path];
            }

            using (var session = _documentStore.OpenSession())
            {
                return session.Load<T>(path);
            }
        }

        public void Put(string path, object item)
        {
            _writeCache[path] = item;
            _cache[path] = item;
        }

        public void Reset()
        {
        }
    }
}