using System;

namespace ESPlus.Subscribers
{
    public interface IEventFetcher
    {
        EventStream GetFromPosition(byte[] position);
        void OnEventReceived(Action action);
    }
}