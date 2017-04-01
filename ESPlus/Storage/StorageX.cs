using System;
using System.Collections.Generic;

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

        // public void ShardOn<T>(int parts)
        // {
        //     _shardMap[typeof (T)] = parts;
        // }

        // public void Patch<T>(string path, Action<T> action)
        //     where T : new()
        // {
        //     var graph = Get<T>(path);

        //     if (graph == null)
        //     {
        //         graph = new T();
        //     }

        //     action(graph);
        //     Put<T>(path, graph);
        // }

        public T Get<T>(string path)
            where T : new()
        {
            return (T) _journal.Get(path);
        }

        public void Put<T>(string path, T graph)
        {
            _journal.Put(path, graph);
        }

        public void Flush()
        {
            _journal.Flush();
        }
    }
}