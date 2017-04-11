using System;
using System.Collections.Generic;
using System.Text;

namespace ESPlus.Storage
{
    public class StorageX : IStorageX
    {
        private IJournaled _journal;
        private string _namespace;
        private Dictionary<Type, int> _shardMap = new Dictionary<Type, int>();

        public StorageX(IJournaled journal, string @namespace)
        {
            _journal = journal;
            _namespace = @namespace;
        }

        public void ShardOn<T>(int parts)
        {
            _shardMap[typeof (T)] = parts;
        }

        public void Patch<T>(string path, Action<T> action)
            where T : new()
        {
            var graph = Get<T>(Name<T>(path));

            if (graph == null)
            {
                graph = new T();
            }

            action(graph);
            Put<T>(Name<T>(path), graph);
        }

        public T Get<T>(string path)
            where T : new()
        {
            return (T) _journal.Get(Name<T>(path));
        }

        public void Put<T>(string path, T graph)
        {
            _journal.Put(Name<T>(path), graph);
        }

        public void Flush()
        {
            _journal.Flush();
        }
        
        private string Name<T>(string input)
        {
            if (!_shardMap.ContainsKey(typeof (T)))
            {
                return input;
            }

            var parts = _shardMap[typeof (T)];
            var bitsPerCharacter = 4;
            var chars = 0;
            var hash = Checksum(input);

            while (bitsPerCharacter << chars >= parts)
            {
                ++chars;
            }

            return $"{hash.Substring(chars)}_input";
        }

        private string Checksum(string input)
        {
            using (var provider = System.Security.Cryptography.SHA1.Create())
            {
                var bytes = provider.ComputeHash(Encoding.UTF8.GetBytes(input));

                return BitConverter.ToString(bytes).Replace("-", "");
            }
        }
    }
}