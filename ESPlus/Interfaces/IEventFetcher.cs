using System;

namespace ESPlus.Subscribers
{
    public interface IEventFetcher
    {
        EventStream GetFromPosition(Position position);
        void OnEventReceived(Action action);
    }
}