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

        public void Flush()
        {
            var collection = _mongoDatabase.GetCollection<HasObjectId>(_collection);
            var pageSize = 100;

            // for (var i = 0; ; ++i)
            // {
            //     // Console.WriteLine($"Page: {i * 100}/{_writeCache.Count()}");
            //     var page = _writeCache.Skip(i * pageSize).Take(pageSize);

            //     if (!page.Any())
            //     {
            //         break;
            //     }
            //var updates = page.Select(d =>

                var updates = _writeCache.Select(d =>
                {
                    var filter = new BsonDocument
                    {
                        {"_id", d.Key},
                        {"_t", d.Value.GetType().Name}
                    };
                    return new ReplaceOneModel<HasObjectId>(filter, d.Value) { IsUpsert = true };
                });

                Retry(() => collection.BulkWrite(updates));
            // }

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
                catch (Exception)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(1 << tries));
                }
            }
        }
    }
}