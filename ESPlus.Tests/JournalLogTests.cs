using System.Collections.Generic;
using ESPlus.Storage;
using ExpectedObjects;
using Newtonsoft.Json;
using Xunit;

namespace ESPlus.Tests
{
    public class JournalLogTests
    {
        private readonly JournalLog _log;

        public JournalLogTests()
        {
            _log = new JournalLog
            {
                Checkpoint = Position.Gen(42),
                Deletes = new HashSet<string>
                {
                    "adam", "jesper"
                },
                Map = new Dictionary<string, string>
                {
                    ["adam"] = "jesper"
                }
            };
        }
        
        [Fact]
        public void JournalLog_PreservedDuringSerialization_True()
        {
            var json = JsonConvert.SerializeObject(_log);
            var deserialized = JsonConvert.DeserializeObject<JournalLog>(json);

            deserialized.ToExpectedObject().ShouldEqual(_log);
        }        
    }
}