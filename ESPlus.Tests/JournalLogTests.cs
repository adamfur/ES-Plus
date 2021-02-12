using System.Collections.Generic;
using ESPlus.Interfaces;
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
                Deletes = new HashSet<StringPair>
                {
                    new StringPair("adam", "tenet"),
                    new StringPair("jesper", "tenet"),
                },
                Map = new Dictionary<StringPair, string>
                {
                    [new StringPair("adam", null)] = "jesper"
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