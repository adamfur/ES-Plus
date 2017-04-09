using System;
using System.Collections.Generic;

namespace ESPlus.Misc
{
    public static class StringEx
    {
        public static readonly List<object> _mutexes = new List<object>();

        static StringEx()
        {
            for (var i = 0; i < 250; ++i)
            {
                _mutexes.Add(new object());
            }
        }

        public static object Intern(string str)
        {
            var offset = Math.Abs(str.GetHashCode() * 997) % _mutexes.Count;

            return _mutexes[offset];
        }
    }
}