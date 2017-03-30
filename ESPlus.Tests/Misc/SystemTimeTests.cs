using System;
using Xunit;

namespace ESPlus.Tests.Misc
{
    public class SystemTimeTests
    {
        private DateTime _date1;
        private DateTime _date2;

        public SystemTimeTests()
        {
            _date1 = new DateTime(1984, 5, 25);
            _date2 = new DateTime(1986, 4, 17);
        }

        [Fact]
        public void UtcNow_NoScope_AsDateTime()
        {
            var diff = SystemTime.UtcNow.Subtract(DateTime.UtcNow).Milliseconds;

            Assert.True(diff < 5);
        }

        [Fact]
        public void UtcNow_WithScope_FixedValue()
        {
            using (new AmbientSystemTimeScope(() => _date1))
            {
                Assert.Equal(_date1, SystemTime.UtcNow);
            }
        }

        [Fact]
        public void UtcNow_ExpiredScope_AsDateTime()
        {
            using (new AmbientSystemTimeScope(() => _date1))
            {
            }

            var diff = SystemTime.UtcNow.Subtract(DateTime.UtcNow).Milliseconds;

            Assert.True(diff < 10);
        }

        [Fact]
        public void UtcNow_NestedScope_FixedValue()
        {
            using (new AmbientSystemTimeScope(() => _date1))
            {
                using (new AmbientSystemTimeScope(() => _date2))
                {
                    Assert.Equal(_date2, SystemTime.UtcNow);
                }
            }
        }

        [Fact]
        public void UtcNow_RecoveringFromNestedScope_FixedValue()
        {
            using (new AmbientSystemTimeScope(() => _date1))
            {
                using (new AmbientSystemTimeScope(() => _date2))
                {
                }
                Assert.Equal(_date1, SystemTime.UtcNow);
            }
        }
    }
}