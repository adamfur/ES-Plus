using System.Threading;

namespace ESPlus.Subscribers
{
    public interface ISubscriptionManager
    {
        ISubscriptionClient Subscribe(Position position, CancellationToken cancellationToken, Priority priority = Priority.Normal);
        void Start();
    }
}
