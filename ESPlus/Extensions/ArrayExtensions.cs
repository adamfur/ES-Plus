using System;

namespace ESPlus.Extentions
{
    public static class ArrayExtensions
    {
        public static byte[] Slice(this byte[] source, int offset, int length)
        {
            var dest = new byte[length];
            
            Array.Copy(source, offset, dest, 0, length);
            return dest;
        }
    }
}