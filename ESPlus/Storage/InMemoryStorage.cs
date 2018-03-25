using System.Collections.Generic;
using ESPlus.Interfaces;

namespace ESPlus.Storage
{
    public class InMemoryStorage : IStorage
    {
        private readonly Dictionary<string, object> _data = new Dictionary<string, object>();

        public void Flush()
        {
        }

        public T Get<T>(string path)
        {
            if (_data.ContainsKey(path))
            {
                return (T) _data[path];
            }
            return default (T);
        }

        public void Put(string path, object item)
        {
            _data[path] = item;
        }

        public void Reset()
        {
            _data.Clear();
        }
    }
}