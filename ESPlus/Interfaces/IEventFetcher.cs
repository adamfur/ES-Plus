using System;
using EventStore.ClientAPI;

namespace ESPlus.Subscribers
{
    public interface IEventFetcher
    {
        EventStream GetFromPosition(Position position);
        void OnEventReceived(Action action);
    }
}