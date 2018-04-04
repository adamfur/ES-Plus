using System.Collections.Generic;
using ESPlus.Interfaces;
using Raven.Client.Documents;

namespace ESPlus.Storage.Raven
{
    public class RavenStorage : IStorage
    {
        private readonly IDocumentStore _documentStore;
        private Dictionary<string, object> _writeCache = new Dictionary<string, object>();

        public RavenStorage(IDocumentStore documentStore)
        {
            _documentStore = documentStore;
        }

        public void Flush()
        {
            using (var session = _documentStore.OpenSession())
            {
                foreach (var item in _writeCache)
                {
                    session.Store(item.Value, item.Key);
                    session.SaveChanges();
                }
            }
            _writeCache.Clear();
        }

        public T Get<T>(string path)
        {
            using (var session = _documentStore.OpenSession())
            {
                return session.Load<T>(path);
            }
        }

        public void Put(string path, object item)
        {
            _writeCache[path] = item;
        }

        public void Reset()
        {
        }
    }
}