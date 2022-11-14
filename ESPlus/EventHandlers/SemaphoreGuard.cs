using System;
using System.Threading;
using System.Threading.Tasks;

namespace ESPlus.EventHandlers
{
    public class SemaphoreGuard : IAsyncDisposable
    {
        private readonly SemaphoreSlim _semaphore;

        private SemaphoreGuard(SemaphoreSlim semaphore)
        {
            _semaphore = semaphore;
        }

        public static async Task<IAsyncDisposable> Build(SemaphoreSlim semaphore)
        {
            var guard = new SemaphoreGuard(semaphore);

            await semaphore.WaitAsync();
            return guard;
        }

        public ValueTask DisposeAsync()
        {
            _semaphore.Release(1);
            return ValueTask.CompletedTask;
        }
    }
}