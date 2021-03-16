using System;
using System.Threading;
using System.Threading.Tasks;

namespace ESPlus
{
    public static class Retry
    {
        public static async Task<T> RetryAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken,
            int tries = 3)
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

                    await Delay(iteration, cancellationToken);
                }
            }

            throw exception;
        }

        public static async Task RetryAsync(Func<Task> action, CancellationToken cancellationToken, int tries = 3)
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

                    await Delay(iteration, cancellationToken);
                }
            }

            throw exception;
        }        
        
        private static async Task Delay(int iteration, CancellationToken cancellationToken)
        {
            if (iteration == 0)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100 << (iteration - 1)), cancellationToken);
        }
    }
}