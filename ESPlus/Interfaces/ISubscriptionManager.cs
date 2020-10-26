using System.Threading;

namespace ESPlus.Subscribers
{
    public interface ISubscriptionManager
    {
        ISubscriptionClient Subscribe(Position position);
    }
}
