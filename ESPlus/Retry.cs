using System;
using System.Threading.Tasks;

namespace ESPlus
{
    public static class Retry
    {
        public static async Task RetryAsync(Func<Task> action, int times = 3)
        {
            Exception exception = null;
            
            for (var iteration = 0; iteration < times; ++iteration)
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
        
        public static async Task RetryAsync(Action action, int times = 3)
        {
            Exception exception = null;
            
            for (var iteration = 0; iteration < times; ++iteration)
            {
                try
                {
                    action();
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