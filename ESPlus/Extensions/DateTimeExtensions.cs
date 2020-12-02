using System;

namespace ESPlus.Extensions
{
    public static class DateTimeExtensions
    {
        public static long ToUnixTime(this DateTime value)
        {
            return (long) value.ToUniversalTime().Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
        }
    }
}