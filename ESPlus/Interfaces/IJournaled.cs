using ESPlus.EventHandlers;
using ESPlus.Interfaces;

namespace ESPlus.Storage
{
    public interface IJournaled : IStorage
    {
        string Checkpoint { get; set; }
        SubscriptionMode SubscriptionMode { get; }
        void Initialize();
    }
}