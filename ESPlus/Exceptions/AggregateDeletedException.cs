using System;

namespace ESPlus
{
    public class AggregateDeletedException : AggregateException
    {
        public string Id { get; private set; }
        public Type Type { get; private set; }

        public AggregateDeletedException(string id, Type type)
        {
            Id = id;
            Type = type;
        }
    }
}