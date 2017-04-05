using EventStore.ClientAPI;

namespace ESPlus.Subscribers
{
    public interface ISubscriptionManager
    {
        ISubscriptionClient Subscribe(Position position, Priority priority = Priority.Normal);
    }
    }
