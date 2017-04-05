using System.Collections.Generic;
using EventStore.ClientAPI;

namespace ESPlus.Subscribers
{
    public interface IEventFetcher
    {
        EventStream GetFromPosition(Position position);
    }
}