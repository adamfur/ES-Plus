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
        private Dictionary<string, HasObjectId> _writeCache = new Dictionary<string, HasObjectId>();
        private Dictionary<string, HasObjectId> _cache = new Dictionary<string, HasObjectId>();

        public MongoStorage(IMongoDatabase mongoDatabase, string collection)
        {
            _mongoDatabase = mongoDatabase;
            this._collection = collection;
        }

        public void Flush()
        {
            var collection = _mongoDatabase.GetCollection<HasObjectId>(_collection);

            for (var i = 0; ; ++i)
            {
                var page = _writeCache.Skip(i * 30).Take(30);

                if (!page.Any())
                {
                    break;
                }

                // var bulkOps = new List<WriteModel<HasObjectId>>();

                // foreach (var item in page)
                // {
                //     var document = item.Value;
                //     var upsertOne = new ReplaceOneModel<HasObjectId>(Builders<HasObjectId>.Filter.Where(x => x.ID == document.ID), document) { IsUpsert = true };

                //     bulkOps.Add(upsertOne);
                // }

                // collection.BulkWrite(bulkOps);

                foreach (var item in page)
                {
                    var document = item.Value;
                    
                    try
                    {
                        collection.ReplaceOne(f => f.ID == document.ID, document, new UpdateOptions
                        {
                            IsUpsert = true
                        });
                    }
                    catch (System.Exception)
                    {
                    }
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
            var collection = _mongoDatabase.GetCollection<T>(_collection);
            var id = ObjectId.Parse(path.MongoHash());
            var result = (T)collection.Find(f => f.ID.Equals(id)).FirstOrDefault();

            // Console.WriteLine($"xxx FOUND: {path} {JsonConvert.SerializeObject(result)}");
            return result;
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