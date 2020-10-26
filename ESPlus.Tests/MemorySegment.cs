using System;
using ESPlus.Magic.Memory;
using Xunit;

namespace ESPlus.Tests
{
    public class MemorySegment
    {
        private readonly byte[] _bytes;
        private readonly long[] _longs;

        public MemorySegment()
        {
            _bytes = new byte[] {0, 1, 2, 3, 4, 5, 6, 7};
            _longs = new long[] {0, 1, 2, 3, 4, 5, 6, 7};
        }
        
        [Fact]
        public void Array_To_MemorySegment()
        {
            Assert.Equal(_bytes, _bytes.AsMemorySegment().AsSpan().ToArray());
        }
        
        [Fact]
        public void Span_To_MemorySegment()
        {
            Assert.Equal(_bytes, _bytes.AsSpan().AsMemorySegment().AsSpan().ToArray());
        }
        
        [Fact]
        public void LongArray_To_MemorySegment()
        {
            Assert.Equal(_longs, _longs.AsMemorySegment().AsSpan().ToArray());
        }
    }
}