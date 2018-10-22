using System;
using System.Collections.Generic;
using System.Threading;

namespace ESPlus.Subscribers
{
    public class SubscriptionContext : IComparable<SubscriptionContext>
    {
        public RequestStatus RequestStatus { get; set; }
        public byte[] Position { get; set; }
        public SubscriptionManager Manager { get; set; }
        public byte[] Future { get; set; }
        public Action<Action> SynchronizedAction { get; set; }
        public CancellationToken CancellationToken { get; set; }
        public bool Ahead { get; private set; } = false;

        public int CompareTo(SubscriptionContext other)
        {
            return 0;
        }
    }
}