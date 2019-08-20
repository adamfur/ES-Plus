using System;
using ESPlus.Storage;
using MongoDB.Driver;
using Xunit;

namespace ESPlus.IntegrationTests.Storage
{
    public class MongoStorageTests
    {
        public class Dummy : HasObjectId, IEquatable<Dummy>
        {
            public bool Equals(Dummy other)
            {
                return ID == other.ID;
            }
        }

        private MongoClient _mongo;
        private IMongoDatabase _database;
        private MongoStorage _storage;
        private Dummy _payload;
        private string _id;

        public MongoStorageTests()
        {
            _id = Guid.NewGuid().ToString();
            _mongo = new MongoClient("mongodb://localhost:27017");
            _database = _mongo.GetDatabase("pliance");
            _storage = new MongoStorage(_database, "temp");
            _payload = new Dummy();
        }

        [Fact]
        public void Get_AfterPut_Equal()
        {
            _storage.Put(_id, _payload);
            
            var result = _storage.Get<Dummy>(_id);
            
            Assert.Equal(_payload, result);
        }
        
        [Fact]
        public void Get_AfterFlush_Equal()
        {
            _storage.Put(_id, _payload);
            _storage.Flush();
            
            var result = _storage.Get<Dummy>(_id);
            
            Assert.Equal(_payload, result);
        }      
        
        [Fact]
        public void Get_AfterDelete_Equal()
        {
            _storage.Put(_id, _payload);
            _storage.Flush();
            _storage.Delete(_id);
            
            var result = _storage.Get<Dummy>(_id);
            
            Assert.Null(result);
        }   
        
        [Fact]
        public void Get_AfterDeleteAndFlush_Equal()
        {
            _storage.Put(_id, _payload);
            _storage.Flush();
            _storage.Delete(_id);
            _storage.Flush();
            
            var result = _storage.Get<Dummy>(_id);
            
            Assert.Null(result);
        }   
        
        [Fact]
        public void Delete_DeleteAndPut_Equal()
        {
            _storage.Delete(_id);
            _storage.Put(_id, _payload);
            _storage.Flush();
            
            var result = _storage.Get<Dummy>(_id);
            
            Assert.NotNull(result);
        }   
        
        [Fact]
        public void Delete_PutAndDelete_Equal()
        {
            _storage.Put(_id, _payload);
            _storage.Delete(_id);
            _storage.Flush();
            
            var result = _storage.Get<Dummy>(_id);
            
            Assert.Null(result);
        }          
    }
}