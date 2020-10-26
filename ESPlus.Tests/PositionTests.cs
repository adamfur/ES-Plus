using ExpectedObjects;
using Newtonsoft.Json;
using Xunit;

namespace ESPlus.Tests
{
    public class PositionTests
    {
        private readonly Position _position;

        public PositionTests()
        {
            _position = Position.Gen(42);
        }

        [Fact]
        public void Position_PreservedDuringSerialization_True()
        {
            var json = JsonConvert.SerializeObject(_position);
            var deserialized = JsonConvert.DeserializeObject<Position>(json);

            deserialized.ToExpectedObject().ShouldEqual(_position);
        }
    }
}