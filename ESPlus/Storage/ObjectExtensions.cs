using System.Collections.Generic;

namespace ESPlus.Storage
{
    public static class ObjectExtensions
    {
        public static List<T> AsList<T>(this T item)
        {
            return new List<T> {item};
        }
    }
}