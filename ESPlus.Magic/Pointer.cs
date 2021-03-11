using System;
using System.Runtime.CompilerServices;

namespace ESPlus.Magic
{
    public static class Pointer
    {
        public static long AsPointer(this object o)
        {
            unsafe
            {
                return (long) Unsafe.AsPointer(ref o);
            }
        }
    }
}