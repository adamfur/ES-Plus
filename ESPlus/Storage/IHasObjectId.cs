using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ESPlus.Storage
{
    public class HasObjectId
    {
        [BsonId]
        public ObjectId ID { get; set; }
    }
}