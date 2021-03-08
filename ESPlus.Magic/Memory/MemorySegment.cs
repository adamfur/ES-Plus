using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace ESPlus.Magic.Memory
{
    public unsafe class MemorySegment<T> : IEnumerable<T>
        where T : IComparable
    {
        private readonly byte* _ptr;
        private readonly int _length;
        private readonly int _adjustedLength;
        private int _offset;
        
        public MemorySegment(byte *ptr, int length)
        {
            _ptr = ptr;
            _length = length;
            _adjustedLength = _length / SizeOf<T>();
        }

        public Type Type()
        {
            return typeof(T);
        }

        public override string ToString()
        {
            var text = new ReadOnlySpan<byte>(_ptr, _length);
            
            return Encoding.UTF8.GetString(text);
        }

        public static MemorySegment<T> Empty()
        {
            return new MemorySegment<T>(null, 0);
        }

        public MemorySegment<E> As<E>()
            where E : IComparable
        {
            return new MemorySegment<E>(_ptr, _length);
        }

        public MemorySegment<T> SliceAt(int length)
        {
            var result = Slice(_offset, length);
            
            _offset += length;

            return result;
        }
        
        public MemorySegment<T> SliceSection()
        {
            var length = To<int>();
            var result = SliceAt(length);
            
            return result;
        }
        
        public MemorySegment<T> SliceAt()
        {
            var result = Slice(_offset, _length - _offset);
            
            _offset = _length;
            return result;
        }

        public MemorySegment<T> Slice(int start)
        {
            var offset = start * SizeOf<T>();

            if (offset > _length)
            {
                throw new ArgumentOutOfRangeException();
            }

            return Slice(start, _length - start);
        }

        public MemorySegment<T> Slice(int start, int length)
        {
            var begin = start * SizeOf<T>();
            var end = length * SizeOf<T>();
            
            if (begin + end > _length)
            {
                throw new ArgumentOutOfRangeException();
            }
            
            if (begin > _length || end > (_length - begin))
            {
                throw new ArgumentOutOfRangeException();
            }

            return new MemorySegment<T>(_ptr + begin, end);
        }

        public int Length => _adjustedLength;
        
        public bool IsEmpty => _offset >= _length;
        public int Offset => _offset;

        public ref T this[int index]
        {
            get
            {
                var offset = index * SizeOf<T>();

                return ref Unsafe.AsRef<T>(_ptr + offset);
            }
        }

        public ref E To<E>()
        {
            if (_length < SizeOf<E>())
            {
                throw new NotImplementedException();
            }

            var addr = _ptr + _offset;

            _offset += SizeOf<E>();
            
            return ref Unsafe.AsRef<E>(addr);
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (var item = 0; item < _length / SizeOf<T>(); ++item)
            {
                yield return this[item];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool Contains(T value)
        {
            var hint = -1;

            return Contains(ref hint, value); 
        }
        
        public bool Contains(ref int hint, T value)
        {
            var max = Length;

            if (max - hint < 50)
            {
                for (var index = hint; index < max; ++index)
                {
                    if (value.CompareTo(this[index]) == 0)
                    {
                        hint = index;
                        return true;
                    }
                }

                return false;
            }

            var left = 0;
            var right = max;

            if (hint != -1)
            {
                var iter = 0;
                
                left = hint;
                right = left;
                
                while (right < max && 0 < value.CompareTo(this[right]))
                {
                    right = left + (1 << iter++);
                }
                
                if (right > max)
                {
                    right = max;
                }
            }
            
            while (left <= right)
            {
                var mid = left + (right - left) / 2;
                var result = value.CompareTo(this[mid]);
            
                if (result == 0)
                {
                    hint = left;
                    return true;
                }
                else if (result < 0)
                {
                    right = mid - 1;
                }
                else
                {
                    left = mid + 1;
                }
            }
            
            return false;
        }

        public Span<T> AsSpan()
        {
            return new Span<T>(_ptr, _length/SizeOf<T>());
        }

        private int SizeOf<E>()
        {
            return System.Runtime.CompilerServices.Unsafe.SizeOf<E>();
        }
    }
}