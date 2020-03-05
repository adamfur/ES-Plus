using System;
using System.Text;

namespace ESPlus.Wyrm
{
    public class Tokenizer
    {
        private static readonly DateTime Epooch = new DateTime(1970, 1, 1);
        private readonly Memory<byte> _data;
        private int _offset;

        public Tokenizer(Memory<byte> data)
        {
            _data = data;
        }

        public int ReadI32()
        {
            var data = ReadBinary(sizeof(Int32));
            
            return BitConverter.ToInt32(data.Span);
        }

        public long ReadI64()
        {
            var data = ReadBinary(sizeof(Int64));
            
            return BitConverter.ToInt64(data.Span);
        }

        public Guid ReadGuid()
        {
            var data = ReadBinary(16);

            return new Guid(data.Span);
        }

        public DateTime ReadDateTime()
        {
            var seconds = ReadI64();
            var milliseconds = ReadI64();
            var time = Epooch.AddSeconds(seconds)
                .AddMilliseconds(milliseconds);
            
            return time;
        }

        public string ReadString()
        {
            var binary = ReadBinary();
            
            return Encoding.UTF8.GetString(binary.Span);
        }

        public Memory<byte> ReadBinary()
        {
            var length = ReadI32();

            return ReadBinary(length);
        }

        public bool Eof()
        {
            return _offset == _data.Length;
        }

        public void AssertEof()
        {
            if (!Eof())
            {
                throw new NotImplementedException("AssertEof");
            }
        }

        public Memory<byte> ReadBinary(int length)
        {
            if (length == 0)
            {
                return Memory<byte>.Empty;
            }
            
            var next = _offset + length;

            if (next > _data.Length)
            {
                throw new OverflowException($"Offset: {_offset}, Read: {length}, Length: {_data.Length}");                
            }

            var range = _data.Slice(_offset, length);

            _offset = next;
            return range;
        }
    }
}