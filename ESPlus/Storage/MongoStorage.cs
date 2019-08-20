using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private readonly Dictionary<ObjectId, HasObjectId> _upserts = new Dictionary<ObjectId, HasObjectId>();
        private readonly HashSet<ObjectId> _deletes = new HashSet<ObjectId>();

        public MongoStorage(IMongoDatabase mongoDatabase, string collection)
        {
            _mongoDatabase = mongoDatabase;
            _collection = collection;
        }

        public void Delete(string path)
        {
//            Console.WriteLine($"Mongo::Delete {path}");
            var id = ObjectId.Parse(path.MongoHash());

            _deletes.Add(id);
            _upserts.Remove(id);
        }

        public void Flush()
        {
            IMongoCollection<HasObjectId> collection = _mongoDatabase.GetCollection<HasObjectId>(_collection);
            var bulk = new List<WriteModel<HasObjectId>>();
//            var watch = Stopwatch.StartNew();

            bulk.AddRange(AssembleUpserts());
            bulk.AddRange(AssembleDeletes());

            if (!bulk.Any())
            {
                return;
            }

            Retry(() => collection.BulkWrite(bulk));

            _deletes.Clear();
            _upserts.Clear();

//            Console.WriteLine($"Mongo:Flush, latency: {watch.ElapsedMilliseconds} ms");
        }

        private IEnumerable<DeleteOneModel<HasObjectId>> AssembleDeletes()
        {
            return _deletes.Select(d =>
            {
                var filter = new BsonDocument
                {
                    {"_id", d}
                };

                return new DeleteOneModel<HasObjectId>(filter);
            });
        }

        private IEnumerable<WriteModel<HasObjectId>> AssembleUpserts()
        {
            return _upserts.Select(d =>
            {
                var filter = new BsonDocument
                {
                    {"_id", d.Key},
//                    {"_t", d.Value.GetType().Name}
                };

                return (WriteModel<HasObjectId>) new ReplaceOneModel<HasObjectId>(filter, d.Value)
                        {IsUpsert = true};
            });
        }

        public T Get<T>(string path)
            where T : HasObjectId
        {
            var id = ObjectId.Parse(path.MongoHash());

            if (_deletes.Contains(id))
            {
                return default;
            }

            if (_upserts.ContainsKey(id))
            {
                return (T) _upserts[id];
            }
            
            var collection = _mongoDatabase.GetCollection<T>(_collection);
            var filter = new BsonDocument
            {
                {"_id", id},
//                {"_t", typeof(T).Name}
            };

            var result = collection.Find(filter).FirstOrDefault();

            return result;
        }

        public void Put(string path, HasObjectId item)
        {
            var id = ObjectId.Parse(path.MongoHash());

            item.ID = id;
            _upserts[id] = item;
            _deletes.Remove(id);
        }

        public void Reset()
        {
        }

        private void Retry(Action action)
        {
            Exception exception = null;
            
            for (var tries = 0; tries < 3; ++tries)
            {
                try
                {
                    action();
                    return;
                }
                catch (OutOfMemoryException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    exception = ex;
                    Thread.Sleep(TimeSpan.FromSeconds(1 << tries));
                }
            }

            throw exception;
        }
    }
}