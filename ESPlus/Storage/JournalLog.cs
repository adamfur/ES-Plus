using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ESPlus.Storage
{
    public class JournalLog : HasObjectId
    {
        public Position Checkpoint { get; set; } = Position.Begin;
        public Dictionary<string, string> Map = new Dictionary<string, string>();
        public HashSet<string> Deletes { get; set; } = new HashSet<string>();
    }
}