using EventStore.ClientAPI;

namespace ESPlus.Subscribers
{
    public class EventFetcherCache : IEventFetcher
    {
        private IEventFetcher _concrete;

        public EventFetcherCache(IEventFetcher concrete)
        {
            _concrete = concrete;
        }

        public EventStream GetFromPosition(Position position)
        {
            return _concrete.GetFromPosition(position);
        }
    }
}