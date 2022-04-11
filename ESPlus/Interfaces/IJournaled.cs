using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ESPlus.EventHandlers;

namespace ESPlus.Storage
{
    public interface IJournaled
    {
        Position Checkpoint { get; set; }
        SubscriptionMode SubscriptionMode { get; set; }
        Task InitializeAsync(CancellationToken cancellationToken = default);
        Task UpdateAsync<T>(string tenant, string path, Action<T> action, CancellationToken cancellationToken);
        Task FlushAsync(CancellationToken cancellationToken);
        void Put<T>(string tenant, string path, T item);
        void Delete(string tenant, string path);
        Task<T> GetAsync<T>(string tenant, string path, CancellationToken cancellationToken);
        void Reset();
        IAsyncEnumerable<byte[]> SearchAsync(string tenant, long[] parameters, CancellationToken cancellationToken);
        Task<Position> ChecksumAsync(CancellationToken cancellationToken);
        IAsyncEnumerable<byte[]> List(string tenant, int size, int no, CancellationToken cancellationToken);
        Task EvictCache();
    }
}