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
using Newtonsoft.Json;

namespace ESPlus.Storage.Raven
{
    public class MongoStorage : IStorage
    {
        private IMongoDatabase _mongoDatabase;
        private readonly string _collection;
        private Dictionary<ObjectId, HasObjectId> _writeCache = new Dictionary<ObjectId, HasObjectId>();
        private Dictionary<ObjectId, HasObjectId> _cache = new Dictionary<ObjectId, HasObjectId>();

        public MongoStorage(IMongoDatabase mongoDatabase, string collection)
        {
            _mongoDatabase = mongoDatabase;
            this._collection = collection;
        }

        public void Flush()
        {
            var collection = _mongoDatabase.GetCollection<HasObjectId>(_collection);
            var pageSize = 1000;

            for (var i = 0; ; ++i)
            {
                var page = _writeCache.Skip(i * pageSize).Take(pageSize);

                if (!page.Any())
                {
                    break;
                }

                var updates = _writeCache.Select(d =>
                {
                    var filter = new BsonDocument()
                    {
                        {"_id", d.Key},
                        {"_t", d.Value.GetType().Name}
                    };
                    return new ReplaceOneModel<HasObjectId>(filter, d.Value) { IsUpsert = true };
                });

                collection.BulkWrite(updates);
            }

            _writeCache.Clear();
        }

        public T Get<T>(string path)
            where T : HasObjectId
        {
            var id = ObjectId.Parse(path.MongoHash());

            if (_cache.ContainsKey(id))
            {
                return (T)_cache[id];
            }

            var collection = _mongoDatabase.GetCollection<T>(_collection);
            var filter = new BsonDocument
            {
                {"_id", id},
                //{"_t", typeof(T).GetType().Name}
            };

            var result = (T)collection.Find(filter).FirstOrDefault();

            return result;
        }

        public void Put(string path, HasObjectId item)
        {
            var id = ObjectId.Parse(path.MongoHash());

            item.ID = id;
            _writeCache[id] = item;
            _cache[id] = item;
        }

        public void Reset()
        {
        }
    }
}