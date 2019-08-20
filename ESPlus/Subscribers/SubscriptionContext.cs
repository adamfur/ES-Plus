using System;

namespace ESPlus.Subscribers
{
    public class SubscriptionContext : IComparable<SubscriptionContext>
    {
        public Position Position { get; set; }
        public SubscriptionManager Manager { get; set; }
        public Position Future { get; set; }
        public bool Ahead { get; private set; } = false;

        public int CompareTo(SubscriptionContext other)
        {
            return 0;
        }
    }
}