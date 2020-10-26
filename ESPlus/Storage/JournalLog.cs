using System.Collections.Generic;

namespace ESPlus.Storage
{
    public class JournalLog : HasObjectId
    {
        public Position Checkpoint { get; set; } = Position.Start;
        public Dictionary<string, string> Map = new Dictionary<string, string>();
        public HashSet<string> Deletes { get; set; } = new HashSet<string>();
    }
}