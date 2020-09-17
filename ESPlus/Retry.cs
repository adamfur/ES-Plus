using System;
using System.Threading.Tasks;

namespace ESPlus
{
    public static class Retry
    {
        public static async Task<T> RetryAsync<T>(Func<Task<T>> action, int tries = 3)
        {
            Exception exception = null;
            
            for (var iteration = 0; iteration < tries; ++iteration)
            {
                try
                {
                    return await action();
                }
                catch (Exception ex)
                {
                    exception = ex;
                    await Task.Delay(TimeSpan.FromSeconds(1 << iteration));
                }
            }

            throw exception;
        }
        
        public static async Task RetryAsync(Func<Task> action, int tries = 3)
        {
            Exception exception = null;
            
            for (var iteration = 0; iteration < tries; ++iteration)
            {
                try
                {
                    await action();
                    return;
                }
                catch (Exception ex)
                {
                    exception = ex;
                    await Task.Delay(TimeSpan.FromSeconds(1 << iteration));
                }
            }

            throw exception;
        }        
    }
}