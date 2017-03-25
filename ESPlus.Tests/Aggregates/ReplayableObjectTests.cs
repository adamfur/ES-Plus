using System;
using ESPlus.Aggregates;
using Xunit;

namespace ESPlus.Tests.Aggregates
{
    public class ReplayableObjectTests
    {
        private readonly string _id;

        public ReplayableObjectTests()
        {
            _id = Guid.NewGuid().ToString();
        }

        [Fact]
        public void Constructor_Initialize_VerionIs0()
        {
            var aggregate = new ReplayableObject(_id);

            Assert.Equal(0, aggregate.Version);
        }

        //[Fact]
        //public void Constructor_Initialize_IdIsExpected()
        //{
        //    var aggregate = new ReplayableObject(_id);

        //    Assert.Equal(_id, aggregate.Id);
        //}
    }
}