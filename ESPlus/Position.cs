using System;
using System.Runtime.InteropServices;

namespace ESPlus
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Position : IEquatable<Position>, IComparable<Position>
    {
        public UInt64 A;
        public UInt64 B;
        public UInt64 C;
        public UInt64 D;

        public override string ToString() 
        {
            return $"{A}:{B}:{C}:{D}";
        }

        public static readonly Position Start = new Position();

        public bool Equals(Position other)
        {
            return A == other.A
                && B == other.B
                && C == other.C
                && D == other.D;
        }

        public static bool operator ==(Position p1, Position p2)
        {
            return p1.Equals(p2);
        }

        public static bool operator !=(Position p1, Position p2)
        {
            return !p1.Equals(p2);
        }

        public static bool operator <(Position p1, Position p2)
        {
            return p1.CompareTo(p2) < 0;
        }

        public static bool operator >(Position p1, Position p2)
        {
            return p1.CompareTo(p2) > 0;
        }

        public static bool operator <=(Position p1, Position p2)
        {
            return p1.CompareTo(p2) <= 0;
        }

        public static bool operator >=(Position p1, Position p2)
        {
            return p1.CompareTo(p2) >= 0;
        }

        public override bool Equals(Object obj)
        {
            if (obj is Position other)
            {
                return Equals(other);
            }

            return false;
        }

        public int CompareTo(Position other)
        {
            var resultA = A.CompareTo(other.A);

            if (resultA != 0)
            {
                return resultA;
            }

            var resultB = B.CompareTo(other.B);

            if (resultB != 0)
            {
                return resultB;
            }

            var resultC = C.CompareTo(other.C);

            if (resultC != 0)
            {
                return resultC;
            }

            return D.CompareTo(other.D);
        }
    }
}