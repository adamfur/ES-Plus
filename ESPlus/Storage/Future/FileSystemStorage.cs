using System.Collections.Generic;
using System.IO;
using ESPlus.Interfaces;

namespace ESPlus.Storage
{
    public class FileSystemStorage : IStorage
    {
        private readonly string _basePath;
        private readonly Dictionary<string, object> _cache = new Dictionary<string, object>();

        public FileSystemStorage(string basePath)
        {
            _basePath = basePath;
        }

        public void Flush()
        {
            foreach (var item in _cache)
            {
                var filename = item.Key;
                var content = (string) item.Value;

                File.WriteAllText(filename, content);
            }
        }

        public T Get<T>(string path)
        {
            if (_cache.ContainsKey(path))
            {
                return (T) _cache[path];
            }

            return (T) (object) File.ReadAllText(path);
        }

        public void Put(string path, object item)
        {
            _cache[path] = item;
        }
    }
}