using ESPlus.EventHandlers;
using ESPlus.Interfaces;
using EventStore.ClientAPI;

namespace ESPlus.Storage
{
    public interface IJournaled : IStorage
    {
        Position Checkpoint { get; set; }
        SubscriptionMode SubscriptionMode { get; }
        void Initialize();
    }
}