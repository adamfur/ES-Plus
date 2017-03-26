using System.Collections.Generic;

namespace ESPlus.Storage
{
    public class JournalLog
    {
        public long Checkpoint { get; set; }
        public Dictionary<string, string> Map = new Dictionary<string, string>();
    }

    // public class Demo
    // {
    //     public void Test()
    //     {
    //         var persistance = new Persistance(new ReplayableJournal(new SqlCheckpointStorage(), new InMemoryStorage(), new InMemoryStorage()));
            
    //         persistance.Get<object>("hello");
    //         persistance.Put("helloworld", new object());
    //         persistance.Flush();
    //     }
    // }
}