using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using ESPlus.Interfaces;
using ESPlus.Storage.Mongo;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace ESPlus.Storage.Raven
{
    public class MongoStorage : IStorage
    {
        private IMongoDatabase _mongoDatabase;
        private readonly string _collection;
        private Dictionary<string, HasObjectId> _writeCache = new Dictionary<string, HasObjectId>();
        private Dictionary<string, HasObjectId> _cache = new Dictionary<string, HasObjectId>();

        public MongoStorage(IMongoDatabase mongoDatabase, string collection)
        {
            _mongoDatabase = mongoDatabase;
            this._collection = collection;
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

                var collection = _mongoDatabase.GetCollection<HasObjectId>(_collection);

                foreach (var item in page)
                {
                    var document = item.Value;

                    document.ID = ObjectId.Parse(item.Key.MongoHash());
                    collection.ReplaceOne(f => f.ID == document.ID, document, new UpdateOptions
                    {
                        IsUpsert = true
                    });
                }
            }

            _writeCache.Clear();
        }

        public T Get<T>(string path)
            where T : HasObjectId
        {
            if (_cache.ContainsKey(path))
            {
                return (T)_cache[path];
            }

            var collection = _mongoDatabase.GetCollection<HasObjectId>(_collection);
            var id = ObjectId.Parse(path.MongoHash());

            return (T)collection.Find(f => f.ID.Equals(id)).FirstOrDefault();
        }

        public void Put(string path, HasObjectId item)
        {
            item.ID = ObjectId.Parse(path.MongoHash());
            _writeCache[path] = item;
            _cache[path] = item;
        }

        public void Reset()
        {
        }
    }
}