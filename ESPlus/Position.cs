using System;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace ESPlus
{
    public class Position : IEquatable<Position>
    {
        public static Position Start => new Position(new byte[32]);
        private byte[] _data = new byte[32];

        public Position(byte[] input)
        {
            _data = input ?? throw new ArgumentNullException(nameof(input));
        }
        
        public static Position Gen(int value)
        {
            var binary = new byte[32];

            binary[0] = (byte) value;
            return new Position(binary);
        }
        
        public string AsHexString()
        {
            var hex = new StringBuilder(_data.Length * 2);

            foreach (byte b in _data)
            {
                hex.AppendFormat("{0:x2}", b);
            }

            return hex.ToString();
        }

        public byte[] Binary()
        {
            return _data;
        }

        public bool Equals(Position other)
        {
            return _data.SequenceEqual(other._data);
        }
    }
}