using System.Collections.Generic;
using EventStore.ClientAPI;

namespace ESPlus.Storage
{
    public class JournalLog
    {
        public Position Checkpoint { get; set; } = Position.Start;
        public Dictionary<string, string> Map = new Dictionary<string, string>();
    }
}