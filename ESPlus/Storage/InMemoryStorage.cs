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

        public object Get(string path)
        {
            if (_data.ContainsKey(path))
            {
                return _data[path];
            }
            return null;
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