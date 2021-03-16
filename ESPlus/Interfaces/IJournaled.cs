using System;
using System.Threading;
using System.Threading.Tasks;
using ESPlus.EventHandlers;
using ESPlus.Interfaces;

namespace ESPlus.Storage
{
    public interface IJournaled : IStorage
    {
        Position Checkpoint { get; set; }
        SubscriptionMode SubscriptionMode { get; }
        Task InitializeAsync();
        Task UpdateAsync<T>(string path, string tenant, Action<T> action, CancellationToken cancellationToken);
    }
}