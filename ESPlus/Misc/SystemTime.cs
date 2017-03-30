using System;

namespace ESPlus
{
    public class SystemTime
    {
        public static DateTime UtcNow
        {
            get
            {
                var asyncLocal = AmbientSystemTimeScope.AsyncLocal;

                if (asyncLocal.Value != null)
                {
                    return asyncLocal.Value();
                }
                return DateTime.UtcNow;
            }
        }

        public static DateTime UtcToday => UtcNow.Date;
    }
}