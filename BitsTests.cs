using Xunit;

namespace ESPlus.Tests.Misc
{
    public class BitsTests
    {
        [Theory]
        [InlineData(0, 1)]
        [InlineData(16, 1)]
        [InlineData(17, 2)]
        [InlineData(256, 2)]
        [InlineData(257, 3)]
        [InlineData(4096, 3)]
        [InlineData(4097, 4)]
        public void Foo(int parts, int expectedValue)
        {
            var result = Bits.HexRequiredForAmountOfVariants(parts);

            Assert.Equal(expectedValue, result);
        }
    }
}
