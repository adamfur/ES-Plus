using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace ESPlus.Storage
{
    public class HasObjectId
    {
        [JsonIgnore]
        [BsonId]
        public ObjectId ID { get; set; }
    }
}