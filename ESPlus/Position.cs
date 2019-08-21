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
        private byte[] _binary = new byte[32];

        public Position(byte[] input)
        {
            Binary = input;
        }

        public Position()
        {
        }
        
        public static Position Gen(int value)
        {
            var binary = new byte[32];

            binary[0] = (byte) value;
            return new Position(binary);
        }
        
        public string AsHexString()
        {
            var hex = new StringBuilder(_binary.Length * 2);

            foreach (byte b in _binary)
            {
                hex.AppendFormat("{0:x2}", b);
            }

            return hex.ToString();
        }

        public byte[] Binary
        {
            get => _binary;
            set
            {
                if (value is null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                if (value.Length != 32)
                {
                    throw new ArgumentException(nameof(value));
                }
                
                _binary = value;
            }
        }

        public bool Equals(Position other)
        {
            return _binary.SequenceEqual(other._binary);
        }

        public override string ToString()
        {
            return AsHexString();
        }
    }
}