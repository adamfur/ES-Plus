using System;

namespace ESPlus.Subscribers
{
    public class EventFetcher : IEventFetcher
    {
        public EventStream GetFromPosition(Position position)
        {
            throw new NotImplementedException();
        }

        public void OnEventReceived(Action action)
        {
        }
    }
}