using System.Collections.Generic;

namespace ESPlus.Storage
{
    public static class ObjectExtensions
    {
        public static List<T> AsList<T>(this T item)
        {
            if (item is null)
            {
                return new List<T>();
            }
            
            return new List<T> {item};
        }
    }
}