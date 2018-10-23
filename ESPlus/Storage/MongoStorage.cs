using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ESPlus.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ESPlus.Storage
{
    public class MongoStorage : IStorage
    {
        private readonly IMongoDatabase _mongoDatabase;
        private readonly string _collection;
        private readonly Dictionary<ObjectId, HasObjectId> _writeCache = new Dictionary<ObjectId, HasObjectId>();

        public MongoStorage(IMongoDatabase mongoDatabase, string collection)
        {
            _mongoDatabase = mongoDatabase;
            _collection = collection;
        }

        public void Delete(string path)
        {
            var id = ObjectId.Parse(path.MongoHash());

            _writeCache[id] = null;
        }

        public void Flush()
        {
            var collection = _mongoDatabase.GetCollection<HasObjectId>(_collection);
            var updates = _writeCache.Select(d =>
            {
                var filter = new BsonDocument
                {
                    {"_id", d.Key},
                    {"_t", d.Value.GetType().Name}
                };

                if (d.Value != null)
                {
                    return (WriteModel<HasObjectId>) new ReplaceOneModel<HasObjectId>(filter, d.Value) { IsUpsert = true };
                }
                else
                {
                    return (WriteModel<HasObjectId>) new DeleteOneModel<HasObjectId>(filter);
                }
            });

            Retry(() => collection.BulkWrite(updates));

            _writeCache.Clear();
        }

        public T Get<T>(string path)
            where T : HasObjectId
        {
            var id = ObjectId.Parse(path.MongoHash());
            var collection = _mongoDatabase.GetCollection<T>(_collection);
            var filter = new BsonDocument
            {
                {"_id", id},
                {"_t", typeof(T).Name}
            };

            var result = (T)collection.Find(filter).FirstOrDefault();

            return result;
        }

        public void Put(string path, HasObjectId item)
        {
            var id = ObjectId.Parse(path.MongoHash());

            item.ID = id;
            _writeCache[id] = item;
        }

        public void Reset()
        {
        }

        private void Retry(Action action)
        {
            for (var tries = 0; tries < 3; ++tries)
            {
                try
                {
                    action();
                    break;
                }
                catch (OutOfMemoryException)
                {
                    throw;
                }
                catch (Exception)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(1 << tries));
                }
            }
        }
    }
}