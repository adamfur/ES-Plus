using System;
using ESPlus.Magic.Memory;
using Xunit;

namespace ESPlus.Tests
{
    public class MemorySegmentTests
    {
        private readonly int[] _intArray;
        private readonly byte[] _byteArray;
        private readonly byte[] _bytes;
        private readonly long[] _longs;

        public MemorySegmentTests()
        {
            _bytes = new byte[] {0, 1, 2, 3, 4, 5, 6, 7};
            _longs = new long[] {0, 1, 2, 3, 4, 5, 6, 7};
            _intArray = new int[3];
            _byteArray = new byte[7];
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
        
        [Fact]
        public void Length_Verify_SameLength()
        {
            var segment = _intArray.AsMemorySegment();
                
            Assert.Equal(3, segment.Length);
        }        

        [Fact]
        public void Length_Verify_SameType()
        {
            var segment = _intArray.AsMemorySegment();
                
            Assert.Equal(typeof(int), segment.Type());
        }
        
        [Fact]
        public void As_Convert_ExpectedType()
        {
            var segment = _intArray.AsMemorySegment().As<byte>();
                
            Assert.Equal(12, segment.Length);
        }   
        
        [Fact]
        public void As_Convert_ExpectedLength()
        {
            var segment = _intArray.AsMemorySegment().As<byte>();
                
            Assert.Equal(typeof(byte), segment.Type());
        }       
        
        [Fact]
        public void AsInt_Convert_ExpectedType()
        {
            var segment = _byteArray.AsMemorySegment().As<int>();
                
            Assert.Equal(typeof(int), segment.Type());
        }  
        
        [Fact]
        public void AsInt_Convert_ExpectedLength()
        {
            var segment = _byteArray.AsMemorySegment().As<int>();
                
            Assert.Equal(1, segment.Length);
        }  
    }
}