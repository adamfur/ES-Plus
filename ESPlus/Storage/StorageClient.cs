using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace ESPlus.Storage
{
    public class StorageClient : IStorageClient
    {
        private IJournaled _journal;
        private string _namespace;
        private Dictionary<Type, int> _shardMap = new Dictionary<Type, int>();

        public StorageClient(IJournaled journal, string @namespace)
        {
            _journal = journal;
            _namespace = @namespace;
        }

        public void ShardOn<T>(int parts)
        {
            _shardMap[typeof(T)] = parts;
        }

        public void Patch<T>(string path, Action<T> action)
            where T : HasObjectId, new()
        {
            var shard = Name<T>(path);
            var graph = Get<T>(shard);

            if (graph == null)
            {
                graph = new T();
            }

            action(graph);
            Put<T>(shard, graph);
        }

        public void Reset()
        {
            _journal.Reset();
        }

        public T Get<T>(string path)
            where T : HasObjectId, new()
        {
            var shard = Name<T>(path);

            return _journal.Get<T>(shard);
        }

        public void Put<T>(string path, T graph)
            where T : HasObjectId
        {
            var shard = Name<T>(path);

            _journal.Put(shard, graph);
        }

        public void Flush()
        {
            _journal.Flush();
        }

        private string Name<T>(string input)
        {
            if (!_shardMap.ContainsKey(typeof(T)))
            {
                return input;
            }

            var shards = _shardMap[typeof(T)];
            var shard = Math.Abs(input.GetHashCode()) % shards;

            return Regex.Replace(input, "/([^/]*)$", $"/{shard}_$i");
        }

        public void Update<T>(string path, Action<T> action)
            where T : HasObjectId, new()
        {
            T item = Get<T>(path);

            if (item == null)
            {
                item = new T();
            }

            action(item);
            Put<T>(path, item);
        }
    }
}