using System.Collections.Generic;

namespace ESPlus.Storage
{
    public class JournalLog
    {
        public string Checkpoint { get; set; }
        public Dictionary<string, string> Map = new Dictionary<string, string>();
    }
}