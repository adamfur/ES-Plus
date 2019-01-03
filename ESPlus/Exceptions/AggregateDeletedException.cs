using System;

namespace ESPlus
{
    public class AggregateDeletedException : AggregateException
    {
        public string Id { get; }
        public Type Type { get; }

        public AggregateDeletedException(string id, Type type)
        {
            Id = id;
            Type = type;
        }
    }
}