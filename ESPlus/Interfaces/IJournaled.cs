using System;
using ESPlus.EventHandlers;
using ESPlus.Interfaces;

namespace ESPlus.Storage
{
    public interface IJournaled : IStorage
    {
        Position Checkpoint { get; set; }
        SubscriptionMode SubscriptionMode { get; }
        void Initialize();
        void Update<T>(string path, Action<T> action) where T : HasObjectId;
    }
}