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

                    await Delay(iteration);
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

                    await Delay(iteration);
                }
            }

            throw exception;
        }        
        
        private static async Task Delay(int iteration)
        {
            if (iteration == 0)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100 << (iteration - 1)));
        }
    }
}