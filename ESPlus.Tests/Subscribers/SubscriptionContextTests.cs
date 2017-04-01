using System;
using System.Collections.Generic;
using System.Linq;
using ESPlus.Subscribers;
using Xunit;

namespace ESPlus.Tests.Subscribers
{
    public class SubscriptionContextTests
    {
        private SubscriptionContext _idle;
        private SubscriptionContext _low;
        private SubscriptionContext _normal;
        private SubscriptionContext _high;
        private SubscriptionContext _realTime;

        public SubscriptionContextTests()
        {
            _idle = new SubscriptionContext { Priority = Priority.Idle };
            _low = new SubscriptionContext { Priority = Priority.Low };
            _normal = new SubscriptionContext { Priority = Priority.Normal };
            _high = new SubscriptionContext { Priority = Priority.High };
            _realTime = new SubscriptionContext { Priority = Priority.RealTime };

        }

        [Fact]
        public void Sort_ByPriority_Descending()
        {
            var list = new List<SubscriptionContext>
            {
                _idle,
                _low,
                _normal,
                _high,
                _realTime
            };

            list.Sort();

            Assert.Equal(_realTime, list[0]);
            Assert.Equal(_high, list[1]);
            Assert.Equal(_normal, list[2]);
            Assert.Equal(_low, list[3]);
            Assert.Equal(_idle, list[4]);
        }

        [Fact]
        public void Sort_RealTimeAlwaysFirst_True()
        {
            _high.StarvedCycles = 1000000;
            var list = new List<SubscriptionContext>
            {
                _high,
                _realTime
            };

            list.Sort();

            Assert.Equal(_realTime, list[0]);
            Assert.Equal(_high, list[1]);
        }

        [Fact]
        public void Sort_IdleAlwaysLast_True()
        {
            _idle.StarvedCycles = 1000000;
            var list = new List<SubscriptionContext>
            {
                _idle,
                _low,
            };

            list.Sort();

            Assert.Equal(_low, list[0]);
            Assert.Equal(_idle, list[1]);
        }

        [Theory]
        [InlineData(0, 0, false)]
        [InlineData(10, 0, false)]
        [InlineData(11, 0, true)]
        public void Sort_VariousInput_ExpectedResult(int normalStarvation, int highStarvation, bool breakOrder)
        {
            _normal.StarvedCycles = normalStarvation;
            _high.StarvedCycles = highStarvation;
            var list = new List<SubscriptionContext>
            {
                _normal,
                _high,
            };

            list.Sort();

            Console.WriteLine($"Score _normal: {_normal.Score}");
            Console.WriteLine($"Score _high:  {_high.Score}");

            Assert.True(list.First() == (breakOrder ? _normal : _high));
        }
    }
}