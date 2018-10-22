using System.Threading;

namespace ESPlus.Subscribers
{
    public interface ISubscriptionManager
    {
        ISubscriptionClient Subscribe(byte[] position, CancellationToken cancellationToken);
        void Start();
    }
}
