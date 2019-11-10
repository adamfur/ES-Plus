using System;
using System.Text;
using ESPlus.Extentions;

namespace ESPlus.Wyrm
{
    public class Tokenizer
    {
        private static readonly DateTime Epooch = new DateTime(1970, 1, 1);
        private byte[] _data;
        private int _offset;
        private int _length;

        public Tokenizer(byte[] data)
        {
            _data = data;
            _length = data.Length;
        }

        public int ReadI32()
        {
            var data = ReadBinary(sizeof(Int32));
            
            return BitConverter.ToInt32(data);
        }

        public long ReadI64()
        {
            var data = ReadBinary(sizeof(Int64));
            
            return BitConverter.ToInt64(data);
        }

        public Guid ReadGuid()
        {
            var data = ReadBinary(16);

            return new Guid(data);
        }

        public DateTime ReadDateTime()
        {
            var seconds = ReadI64();
            var milliseconds = ReadI64();
            var time = Epooch.AddSeconds(seconds).AddMilliseconds(milliseconds).ToLocalTime();
            
            return time;
        }

        public string ReadString()
        {
            var length = ReadI32();

            return Encoding.UTF8.GetString(ReadBinary(length));
        }

        public bool Eof()
        {
            return _offset == _length;
        }

        public void AssertEof()
        {
            if (!Eof())
            {
                throw new NotImplementedException("AssertEof");
            }
        }

        public byte[] ReadBinary(int length)
        {
            var next = _offset + length;

            if (next > _length)
            {
                throw new OverflowException();
            }

            var range = _data.Slice(_offset, length);

            _offset = next;
            return range;
        }
    }
}