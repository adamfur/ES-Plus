using System;

namespace ESPlus
{
    public class AggregateNotFoundException : AggregateException
    {
        public string Id { get; private set; }
        public Type Type { get; private set; }

        public AggregateNotFoundException(string id, Type type)
        {
            Id = id;
            Type = type;
        }
    }    
}