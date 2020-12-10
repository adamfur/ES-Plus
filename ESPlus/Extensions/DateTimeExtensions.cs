using System;

namespace ESPlus.Extensions
{
    public static class DateTimeExtensions
    {
        public static long ToUnixTime(this DateTime input)
        {
            if (input == default)
            {
                return 0;
            }
            
            return (long) input.ToUniversalTime().Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
        }
    }
}