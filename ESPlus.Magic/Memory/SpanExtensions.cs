using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using LightningDB;

namespace ESPlus.Magic.Memory
{
    public static class SpanExtensions
    {
        private struct MDBValueStructure
        {
            public IntPtr Size;
            public unsafe byte* Data;
        }

        public static unsafe MemorySegment<T> AsMemorySegment<T>(this MDBValue span)
            where T : IComparable
        {
            var ptr = (byte*) Unsafe.AsPointer(ref span);
            var crap = Unsafe.AsRef<MDBValueStructure>(ptr);

            return new MemorySegment<T>(crap.Data, (int) crap.Size);
        }

        public static MemorySegment<T> AsMemorySegment<T>(this T[] span)
            where T : IComparable
        {
            return CreateMemorySegment(ref span[0], span.Length);
        }
        
        public static MemorySegment<T> AsMemorySegment<T>(ref this T span)
            where T : struct, IComparable 
        {
            return CreateMemorySegment(ref span, Marshal.SizeOf<T>());
        }
        
        public static unsafe Span<T> AsSpan<T>(ref this T span)
            where T : struct, IComparable 
        {
            var ptr = (byte*) Unsafe.AsPointer(ref span);

            return new Span<T>(ptr, Marshal.SizeOf<T>());
        }
        
        // public static unsafe Span<E> AsSpan<E, T>(ref this T span)
        //     where T : struct, IComparable 
        // {
        //     var ptr = (byte*) System.Runtime.CompilerServices.Unsafe.AsPointer(ref span);
        //
        //     return new Span<E>(ptr, Marshal.SizeOf<T>());
        // }

        public static MemorySegment<T> AsMemorySegment<T>(this Span<T> span)
            where T : IComparable
        {
            return CreateMemorySegment(ref span[0], span.Length);
        }

        private static unsafe MemorySegment<T> CreateMemorySegment<T>(ref T value, int length)
            where T : IComparable
        {
            var ptr = (byte*) Unsafe.AsPointer(ref value);

            return new MemorySegment<T>(ptr, length * Marshal.SizeOf<T>());
        }
    }
}