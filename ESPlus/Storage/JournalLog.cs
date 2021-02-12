using System.Collections.Generic;
using ESPlus.Interfaces;
using Newtonsoft.Json;

namespace ESPlus.Storage
{
    public class JournalLog
    {
        public Position Checkpoint { get; set; } = Position.Start;
        [JsonConverter(typeof(CustomDictionaryConverter<StringPair, string>))]
        public Dictionary<StringPair, string> Map = new Dictionary<StringPair, string>();
        public HashSet<StringPair> Deletes { get; set; } = new HashSet<StringPair>();
    }
}