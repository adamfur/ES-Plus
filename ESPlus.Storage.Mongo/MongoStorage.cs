using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using ESPlus.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ESPlus.Storage.Raven
{
    public class MongoStorage : IStorage
    {
        private IMongoDatabase _mongoDatabase;
        private Dictionary<string, object> _writeCache = new Dictionary<string, object>();
        private Dictionary<string, object> _cache = new Dictionary<string, object>();

        public MongoStorage(IMongoDatabase mongoDatabase)
        {
            _mongoDatabase = mongoDatabase;
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

                var collection = _mongoDatabase.GetCollection<object>("helloworld");

                foreach (var item in page)
                {
                    // try
                    // {
                    //     collection.DeleteOne(new BsonDocument { { "_id", ((dynamic)item.Value).ID } });
                    // }
                    // catch (Exception ex)
                    // {
                    //     Console.WriteLine(ex);
                    // }

                    // try
                    // {
                    //     collection.InsertOne(item.Value);
                    // }
                    // catch (Exception ex)
                    // {
                    //     Console.WriteLine(ex);
                    // }

                    try
                    {
                        var filter = new BsonDocument { { "_id", ((dynamic)item.Value).ID } };

                        if (collection.Find(filter).Any())
                        {
                            collection.DeleteOne(filter);
                        }
                        collection.InsertOne(item.Value);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
            }

            _writeCache.Clear();
        }

        public T Get<T>(string path)
        {
            if (_cache.ContainsKey(path))
            {
                return (T)_cache[path];
            }

            var collection = _mongoDatabase.GetCollection<T>("helloworld");
            var filter = Builders<T>.Filter.Eq("ReferenceId", path);

            return collection.Find(filter).FirstOrDefault();
        }

        public void Put(string path, object item)
        {
            _writeCache[path] = item;
            _cache[path] = item;
        }

        public void Reset()
        {
        }
    }
}