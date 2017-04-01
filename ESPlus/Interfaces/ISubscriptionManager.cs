namespace ESPlus.Subscribers
{
    public interface ISubscriptionManager
    {
        ISubscriptionClient Subscribe(long position, Priority priority = Priority.Normal);
    }
    }
