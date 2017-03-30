using System;

namespace ESPlus.Storage
{
    public class StorageX : IStorageX
    {
        private IJournaled _journal;
        private string _namespace;

        public StorageX(IJournaled journal, string @namespace)
        {
            _journal = journal;
            _namespace = @namespace;
        }

        public void Update<T>(string path, Action<T> action)
            where T : new()
        {
            var graph = Get<T>(path);

            if (graph == null)
            {
                graph = new T();
            }

            action(graph);
            Put<T>(path, graph);
        }

        public T Get<T>(string path)
            where T : new()
        {
            return new T();
        }

        public void Put<T>(string path, T graph)
        {
        }

        public void Flush()
        {
            throw new NotImplementedException();
        }
    }
}