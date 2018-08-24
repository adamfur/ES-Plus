using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ESPlus.Storage
{
    public class JournalLog
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string ID => "000000000000000000000000";
        public Position Checkpoint { get; set; } = Position.Start;
        public Dictionary<string, string> Map = new Dictionary<string, string>();
    }
}