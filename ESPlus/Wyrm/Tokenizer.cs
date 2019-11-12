using System;
using System.Text;
using ESPlus.Extentions;

namespace ESPlus.Wyrm
{
    public class Tokenizer
    {
        private static readonly DateTime Epooch = new DateTime(1970, 1, 1);
        private readonly byte[] _data;
        private int _offset;

        public Tokenizer(byte[] data)
        {
            _data = data;
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

            if (length == 0)
            {
                return "";
            }

            return Encoding.UTF8.GetString(ReadBinary(length));
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

        public byte[] ReadBinary(int length)
        {
            if (length == 0)
            {
                return new byte[0];
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